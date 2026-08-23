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

public sealed class ChargePostedOutboxHandler : IOutboxMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ApplicationDbContext _db;
    private readonly IJournalService _journals;
    private readonly ITenantModuleService _modules;

    public ChargePostedOutboxHandler(
        ApplicationDbContext db,
        IJournalService journals,
        ITenantModuleService modules)
    {
        _db = db;
        _journals = journals;
        _modules = modules;
    }

    public string MessageType => OutboxMessageTypes.ChargePosted;

    public async Task HandleAsync(string payloadJson, CancellationToken cancellationToken = default)
    {
        if (!await _modules.IsEnabledAsync(TenantModuleKeys.Accounting, cancellationToken))
            return;

        var payload = JsonSerializer.Deserialize<ChargePostedPayload>(payloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Invalid charge.posted payload.");
        if (payload.FeeChargeIds.Count == 0)
            return;

        var charges = await _db.FeeCharges.AsNoTracking()
            .Where(c => payload.FeeChargeIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var charge in charges)
        {
            var exists = await _db.JournalEntries.AsNoTracking().AnyAsync(
                e => e.SourceType == JournalSourceType.FeeCharge && e.SourceId == charge.Id,
                cancellationToken);
            if (exists)
                continue;

            await _journals.PostBalancedAsync(
                charge.HorizontalPropertyId,
                charge.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                JournalSourceType.FeeCharge,
                charge.Id,
                $"Cargo cuota {charge.Id}",
                [
                    (PucSeed.Receivable, charge.Amount, 0m, null),
                    (PucSeed.IncomeAdmin, 0m, charge.Amount, null),
                ],
                cancellationToken);
        }
    }
}
