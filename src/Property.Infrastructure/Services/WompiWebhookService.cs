using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Property.Application.Auditing;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Features.Billing.PaymentIntents;
using Property.Infrastructure.Common;
using Property.Infrastructure.Payments;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class WompiWebhookService : IPspWebhookService
{
    private readonly ApplicationDbContext _db;
    private readonly CurrentTenantContext _tenant;
    private readonly IPaymentIntentService _intents;
    private readonly IAuditWriter _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WompiOptions _options;
    private readonly ILogger<WompiWebhookService> _logger;

    public WompiWebhookService(
        ApplicationDbContext db,
        ICurrentTenantContext tenant,
        IPaymentIntentService intents,
        IAuditWriter audit,
        IUnitOfWork unitOfWork,
        IOptions<WompiOptions> options,
        ILogger<WompiWebhookService> logger)
    {
        _db = db;
        _tenant = (CurrentTenantContext)tenant;
        _intents = intents;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessWompiEventAsync(
        string rawJsonBody,
        string? checksumHeader,
        CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(rawJsonBody);
        var root = doc.RootElement;
        var eventName = root.TryGetProperty("event", out var eventEl) ? eventEl.GetString() : null;
        if (!string.Equals(eventName, "transaction.updated", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Ignoring Wompi event type {Event}", eventName);
            return;
        }

        if (!root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("transaction", out var tx))
            throw new InvalidOperationException("Wompi event missing data.transaction.");

        var txId = tx.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        var reference = tx.TryGetProperty("reference", out var refEl) ? refEl.GetString() : null;
        var status = tx.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

        var intent = await FindIntentAsync(txId, reference, cancellationToken)
            ?? throw new InvalidOperationException("Payment intent was not found for Wompi transaction.");

        _tenant.TenantId = intent.TenantId;

        if (!WompiEventSignature.TryValidate(
                rawJsonBody,
                checksumHeader,
                _options.EventsSecret,
                out var failureReason))
        {
            await _audit.WriteAsync(
                AuditEntityTypes.PspWebhook,
                txId ?? reference,
                AuditActions.PspWebhookInvalidSignature,
                before: null,
                after: new { failureReason, txId, reference },
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(failureReason ?? "Invalid Wompi webhook signature.");
        }

        if (string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            await _intents.MarkPaidAsync(
                intent.Id,
                new MarkPaymentIntentPaidDto
                {
                    ProviderRef = txId ?? intent.ProviderRef,
                    PaidAtUtc = DateTime.UtcNow,
                },
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(status) &&
                 (status.Equals("DECLINED", StringComparison.OrdinalIgnoreCase) ||
                  status.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                  status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)))
        {
            await _intents.MarkFailedAsync(
                intent.Id,
                new MarkPaymentIntentFailedDto { Reason = $"Wompi status: {status}" },
                cancellationToken);
        }

        await _audit.WriteAsync(
            AuditEntityTypes.PspWebhook,
            intent.Id.ToString(),
            AuditActions.PspWebhookProcessed,
            before: null,
            after: new { txId, reference, status, intentId = intent.Id },
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Domain.Entities.PaymentIntent?> FindIntentAsync(
        string? txId,
        string? reference,
        CancellationToken cancellationToken)
    {
        var query = _db.PaymentIntents.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(txId))
        {
            var byRef = await query.FirstOrDefaultAsync(i => i.ProviderRef == txId, cancellationToken);
            if (byRef is not null)
                return byRef;
        }

        if (Guid.TryParse(reference, out var intentId))
        {
            var byId = await query.FirstOrDefaultAsync(i => i.Id == intentId, cancellationToken);
            if (byId is not null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(reference))
        {
            return await query.FirstOrDefaultAsync(
                i => i.IdempotencyKey == reference || i.ProviderRef == reference,
                cancellationToken);
        }

        return null;
    }
}
