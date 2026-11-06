using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Billing.BreB;
using Property.Application.Features.Billing.FeeCharges;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class AdminFeeNoticeTests
{
    private sealed class RecordingQr : IQrCodePngRenderer
    {
        public List<string> Payloads { get; } = [];

        public byte[] Render(string payload)
        {
            Payloads.Add(payload);
            return [0x89, 0x50, 0x4E, 0x47];
        }
    }

    [Fact]
    public async Task Liquidation_queues_one_notice_and_dispatch_sends_one_qr_email()
    {
        using var fx = new TenantDbFixture();
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "Conjunto Río",
            AddressLine = "Calle 1",
            City = "Pasto",
            Country = "Colombia",
            IsActive = true,
        };
        var period = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Year = 2026,
            Month = 8,
            Status = BillingPeriodStatus.Open,
        };
        var billed = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "101",
            UnitType = UnitType.Apartment,
            Coefficient = 40m,
            IsActive = true,
            IsBillable = true,
        };
        var silent = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "102",
            UnitType = UnitType.Apartment,
            Coefficient = 60m,
            IsActive = true,
            IsBillable = true,
        };
        var payer = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "100",
            FirstName = "Ana",
            LastName = "Ríos",
            Email = "ana@example.com",
            IsActive = true,
        };
        var visitor = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "200",
            FirstName = "Luis",
            LastName = "Paz",
            Email = "luis@example.com",
            IsActive = true,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.Units.AddRange(billed, silent);
        fx.Db.FeeSettings.Add(new FeeSettings
        {
            HorizontalPropertyId = property.Id,
            MonthlyBudgetAmount = 1_000_000m,
            DueDayOfMonth = 10,
        });
        fx.Db.BreBPaymentProfiles.Add(new BreBPaymentProfile
        {
            HorizontalPropertyId = property.Id,
            Key = "@conjunto",
            KeyType = 4,
            MerchantName = "Conjunto Rio",
            City = "Pasto",
            PostalCode = "52001",
        });
        fx.Db.People.AddRange(payer, visitor);
        fx.Db.UnitOccupancies.AddRange(
            new UnitOccupancy
            {
                Id = Guid.NewGuid(),
                UnitId = billed.Id,
                PersonId = payer.Id,
                Role = OccupancyRole.Owner,
                ReceivesBilling = true,
                IsActive = true,
                StartDate = new DateOnly(2026, 1, 1),
            },
            new UnitOccupancy
            {
                Id = Guid.NewGuid(),
                UnitId = billed.Id,
                PersonId = visitor.Id,
                Role = OccupancyRole.Resident,
                ReceivesBilling = false,
                IsActive = true,
                StartDate = new DateOnly(2026, 1, 1),
            });
        await fx.Db.SaveChangesAsync();

        var qr = new RecordingQr();
        var outbox = new OutboxWriter(
            fx.Db,
            fx.TenantContext,
            Microsoft.Extensions.Options.Options.Create(new Property.Application.Common.Options.OutboxOptions()));
        var notices = new AdminFeeNoticeService(
            fx.Db,
            outbox,
            new UnitBillingMailDirectory(fx.Db),
            qr,
            NullLogger<AdminFeeNoticeService>.Instance);
        var charges = new FeeChargeService(
            fx.Db,
            fx.Repository<FeeCharge>(),
            fx.Repository<BillingPeriod>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.UnitOfWork,
            outbox,
            notices);

        var result = await charges.CommitLiquidationAsync(new LiquidationCommitDto
        {
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
        });

        Assert.True(result.PaymentQrConfigured);
        Assert.Equal(1, result.PaymentNoticeEmailsQueued);
        Assert.Equal(1, result.PaymentNoticeUnitsWithoutEmail);
        Assert.Equal(1, await fx.Db.OutboxMessages.CountAsync(m => m.MessageType == OutboxMessageTypes.AdminFeeNotice));

        var chargeIds = await fx.Db.FeeCharges.Select(c => c.Id).ToListAsync();
        await notices.DispatchAsync(new AdminFeeNoticePayload
        {
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            FeeChargeIds = chargeIds,
        });
        await fx.Db.SaveChangesAsync();

        var emails = await fx.Db.OutboxMessages
            .Where(m => m.MessageType == OutboxMessageTypes.EmailSend)
            .ToListAsync();
        var email = Assert.Single(emails);
        Assert.Contains("ana@example.com", email.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("cid:cuota-qr", email.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("101202608", email.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("luis@example.com", email.PayloadJson, StringComparison.Ordinal);

        var qrText = Assert.Single(qr.Payloads);
        Assert.Contains("400000.00", qrText, StringComparison.Ordinal);
        Assert.Contains("101202608", qrText, StringComparison.Ordinal);
        Assert.EndsWith(BreBQrPayload.Crc16Ccitt(qrText[..^4]), qrText, StringComparison.Ordinal);

        await notices.DispatchAsync(new AdminFeeNoticePayload
        {
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            FeeChargeIds = chargeIds,
        });
        await fx.Db.SaveChangesAsync();

        Assert.Equal(1, await fx.Db.OutboxMessages.CountAsync(m => m.MessageType == OutboxMessageTypes.EmailSend));
        Assert.Single(qr.Payloads);
        Assert.Equal(1, await fx.Db.AdminFeeNoticeDeliveries.CountAsync());
    }
}
