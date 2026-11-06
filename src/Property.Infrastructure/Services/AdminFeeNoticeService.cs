using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.BreB;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Persistence;

namespace Property.Infrastructure.Services;

public sealed class AdminFeeNoticeService : IAdminFeeNoticeService
{
    private readonly ApplicationDbContext _db;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitBillingMailDirectory _contacts;
    private readonly IQrCodePngRenderer _qrCodes;
    private readonly ILogger<AdminFeeNoticeService> _logger;

    public AdminFeeNoticeService(
        ApplicationDbContext db,
        IOutboxWriter outbox,
        IUnitBillingMailDirectory contacts,
        IQrCodePngRenderer qrCodes,
        ILogger<AdminFeeNoticeService> logger)
    {
        _db = db;
        _outbox = outbox;
        _contacts = contacts;
        _qrCodes = qrCodes;
        _logger = logger;
    }

    public async Task<AdminFeeNoticePlan> ScheduleAsync(
        Guid horizontalPropertyId,
        Guid billingPeriodId,
        IReadOnlyList<AdminFeeNoticeCharge> charges,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.BreBPaymentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.HorizontalPropertyId == horizontalPropertyId, cancellationToken);
        var configurationError = profile is null
            ? BreBQrPayload.MissingKey
            : BreBQrPayload.ConfigurationError(ToProfile(profile));
        if (configurationError is not null)
        {
            return new AdminFeeNoticePlan
            {
                PaymentQrConfigured = false,
                PaymentQrSkipReason = configurationError,
            };
        }

        var payable = charges.Where(charge => charge.Amount > 0m).ToList();
        var contacts = await _contacts.ListByUnitAsync(
            payable.Select(charge => charge.UnitId).Distinct().ToList(),
            cancellationToken);

        var queued = 0;
        var missing = 0;
        foreach (var charge in payable)
        {
            if (!contacts.TryGetValue(charge.UnitId, out var unitContacts) || unitContacts.Count == 0)
                missing++;
            else
                queued += unitContacts.Count;
        }

        if (queued > 0)
        {
            await _outbox.EnqueueAsync(
                OutboxMessageTypes.AdminFeeNotice,
                new AdminFeeNoticePayload
                {
                    HorizontalPropertyId = horizontalPropertyId,
                    BillingPeriodId = billingPeriodId,
                    FeeChargeIds = payable.Select(charge => charge.FeeChargeId).ToList(),
                },
                cancellationToken);
        }

        return new AdminFeeNoticePlan
        {
            PaymentQrConfigured = true,
            PaymentNoticeEmailsQueued = queued,
            PaymentNoticeUnitsWithoutEmail = missing,
        };
    }

    public async Task DispatchAsync(AdminFeeNoticePayload payload, CancellationToken cancellationToken = default)
    {
        if (payload.FeeChargeIds.Count == 0)
            return;

        var profile = await _db.BreBPaymentProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.HorizontalPropertyId == payload.HorizontalPropertyId, cancellationToken);
        if (profile is null || BreBQrPayload.ConfigurationError(ToProfile(profile)) is not null)
        {
            _logger.LogWarning(
                "Skipping administration fee notices for property {PropertyId}: Bre-B profile is incomplete.",
                payload.HorizontalPropertyId);
            return;
        }

        var charges = await (
            from charge in _db.FeeCharges.AsNoTracking()
            join unit in _db.Units.AsNoTracking() on charge.UnitId equals unit.Id
            join period in _db.BillingPeriods.AsNoTracking() on charge.BillingPeriodId equals period.Id
            join property in _db.HorizontalProperties.AsNoTracking() on charge.HorizontalPropertyId equals property.Id
            where payload.FeeChargeIds.Contains(charge.Id)
                && charge.IsActive
                && charge.ChargeType == FeeChargeType.Ordinary
                && charge.Amount > 0m
            select new NoticeCharge(
                charge.Id,
                charge.UnitId,
                unit.Code,
                charge.Amount,
                charge.CoefficientSnapshot,
                charge.DueDate,
                period.Year,
                period.Month,
                property.Name)
        ).ToListAsync(cancellationToken);

        if (charges.Count == 0)
            return;

        var alreadySent = await _db.AdminFeeNoticeDeliveries
            .AsNoTracking()
            .Where(delivery => payload.FeeChargeIds.Contains(delivery.FeeChargeId))
            .Select(delivery => new { delivery.FeeChargeId, delivery.ToEmail })
            .ToListAsync(cancellationToken);
        var sent = alreadySent
            .Select(delivery => (delivery.FeeChargeId, Email: delivery.ToEmail.Trim().ToLowerInvariant()))
            .ToHashSet();

        var contacts = await _contacts.ListByUnitAsync(
            charges.Select(charge => charge.UnitId).Distinct().ToList(),
            cancellationToken);
        var qrProfile = ToProfile(profile);

        foreach (var charge in charges)
        {
            if (!contacts.TryGetValue(charge.UnitId, out var unitContacts))
                continue;

            var pending = unitContacts
                .Where(contact => !sent.Contains((charge.Id, contact.Email.Trim().ToLowerInvariant())))
                .ToList();
            if (pending.Count == 0)
                continue;

            var periodKey = $"{charge.Year:0000}-{charge.Month:00}";
            var reference = AdminFeeQrReference.FromUnitPeriod(charge.UnitCode, charge.Year, charge.Month);
            string qrText;
            try
            {
                qrText = BreBQrPayload.Build(qrProfile, charge.Amount, reference);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skipping administration fee QR for charge {ChargeId}.",
                    charge.Id);
                continue;
            }

            var png = Convert.ToBase64String(_qrCodes.Render(qrText));
            foreach (var contact in pending)
            {
                var (subject, html) = AdminFeeNoticeMessage.Compose(
                    contact.DisplayName,
                    charge.PropertyName,
                    charge.UnitCode,
                    periodKey,
                    charge.Amount,
                    charge.Coefficient,
                    charge.DueDate,
                    reference);

                await _outbox.EnqueueAsync(
                    OutboxMessageTypes.EmailSend,
                    new EmailSendPayload
                    {
                        ToEmail = contact.Email,
                        Subject = subject,
                        HtmlBody = html,
                        InlineContentId = AdminFeeNoticeMessage.QrContentId,
                        InlineFileName = $"cuota-{reference}.png",
                        InlineContentType = "image/png",
                        InlineContentBase64 = png,
                    },
                    cancellationToken);

                await _db.AdminFeeNoticeDeliveries.AddAsync(
                    new AdminFeeNoticeDelivery
                    {
                        Id = Guid.NewGuid(),
                        FeeChargeId = charge.Id,
                        ToEmail = contact.Email.Trim(),
                    },
                    cancellationToken);
                sent.Add((charge.Id, contact.Email.Trim().ToLowerInvariant()));
            }
        }
    }

    private static BreBQrProfile ToProfile(BreBPaymentProfile profile) =>
        new(profile.Key, profile.KeyType, profile.MerchantName, profile.City, profile.PostalCode);

    private sealed record NoticeCharge(
        Guid Id,
        Guid UnitId,
        string UnitCode,
        decimal Amount,
        decimal Coefficient,
        DateOnly DueDate,
        int Year,
        int Month,
        string PropertyName);
}
