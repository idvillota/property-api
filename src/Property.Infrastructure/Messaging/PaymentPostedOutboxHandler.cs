using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Accounting;
using Property.Application.Messaging;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;
using Property.Infrastructure.Services.Accounting;

namespace Property.Infrastructure.Messaging;

public sealed class PaymentPostedOutboxHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApplicationDbContext _db;
    private readonly IJournalService _journals;
    private readonly ITenantModuleService _modules;

    public PaymentPostedOutboxHandler(
        ApplicationDbContext db,
        IJournalService journals,
        ITenantModuleService modules)
    {
        _db = db;
        _journals = journals;
        _modules = modules;
    }

    public string MessageType => OutboxMessageTypes.PaymentPosted;

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        if (!await _modules.IsEnabledAsync(TenantModuleKeys.Accounting, cancellationToken))
            return;

        var payload = JsonSerializer.Deserialize<PaymentPostedPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid payment.posted payload.");

        var payment = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == payload.PaymentId, cancellationToken);
        if (payment is null)
            return;

        var exists = await _db.JournalEntries.AsNoTracking().AnyAsync(
            e => e.SourceType == JournalSourceType.Payment && e.SourceId == payment.Id,
            cancellationToken);
        if (exists)
            return;

        var cashAccount = payment.Method == PaymentMethod.Cash ? PucSeed.Cash : PucSeed.Bank;
        await _journals.PostBalancedAsync(
            payment.HorizontalPropertyId,
            payment.PaidAt,
            JournalSourceType.Payment,
            payment.Id,
            $"Pago {payment.Id}",
            [
                (cashAccount, payment.Amount, 0m, null),
                (PucSeed.Receivable, 0m, payment.Amount, null),
            ],
            cancellationToken);
    }
}
