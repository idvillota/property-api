using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Property.Application.Common.Options;
using Property.Application.Features.Billing.Payments;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Messaging;
using Property.Infrastructure.Services;
using Property.Infrastructure.Storage;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class PaymentReceiptTests
{
    private sealed record Seed(HorizontalProperty Property, DomainUnit Unit, FeeCharge Charge, Person Person);

    private static async Task<Seed> SeedAsync(TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH Recibo",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            IsActive = true,
        };
        var unit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "301",
            UnitType = UnitType.Apartment,
            Coefficient = 1m,
            IsActive = true,
            IsBillable = true,
        };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "1",
            FirstName = "Ana",
            LastName = "R",
            Email = "ana@example.com",
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
        var charge = new FeeCharge
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            UnitId = unit.Id,
            ChargeType = FeeChargeType.Ordinary,
            Description = "Cuota",
            CoefficientSnapshot = 1m,
            Amount = 100_000m,
            AmountPaid = 0m,
            DueDate = new DateOnly(2026, 8, 30),
            Status = FeeChargeStatus.Pending,
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.Add(person);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(charge);
        fx.Db.TenantSettings.Add(new TenantSettings
        {
            TenantId = fx.TenantId,
            TradeName = "PH",
            LegalName = "PH",
            TaxRegime = "RS",
            TaxId = "1",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            EmailReceiptOnPaymentEnabled = true,
        });
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unit, charge, person);
    }

    private static (PaymentService Payments, PaymentReceiptService Receipts, string Root) Create(
        TenantDbFixture fx)
    {
        var root = Path.Combine(Path.GetTempPath(), "receipt-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var files = new FileAssetService(
            fx.Db,
            new LocalDiskFileStorage(root),
            fx.TenantContext,
            fx.UnitOfWork,
            Options.Create(new FileStorageOptions { MaxBytes = 5_000_000, Provider = "Local" }));
        var outbox = new OutboxWriter(fx.Db, fx.TenantContext, Options.Create(new OutboxOptions()));
        var payments = new PaymentService(
            fx.Db,
            fx.Repository<Payment>(),
            fx.Repository<FeeCharge>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<Person>(),
            fx.UnitOfWork,
            new AuditWriter(fx.Db, fx.TenantContext));
        var receipts = new PaymentReceiptService(fx.Db, files, outbox, fx.UnitOfWork);
        return (payments, receipts, root);
    }

    [Fact]
    public async Task ReceiptPdf_Generated_ForPayment()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var (payments, receipts, root) = Create(fx);
        try
        {
            var payment = await payments.CreateAsync(new CreatePaymentDto
            {
                HorizontalPropertyId = seed.Property.Id,
                UnitId = seed.Unit.Id,
                PersonId = seed.Person.Id,
                Amount = 100_000m,
                PaidAt = DateTime.UtcNow,
                Method = PaymentMethod.Cash,
                Reference = "RC-1",
                Allocations = [new CreatePaymentAllocationDto { FeeChargeId = seed.Charge.Id, Amount = 100_000m }],
            });

            var fileId = await receipts.EnsureReceiptAsync(payment.Id, enqueueEmailIfEnabled: false);
            Assert.NotEqual(Guid.Empty, fileId);

            var download = await receipts.DownloadReceiptAsync(payment.Id);
            Assert.NotNull(download);
            Assert.Equal("application/pdf", download!.Asset.ContentType);
            await using var ms = new MemoryStream();
            await download.Content.CopyToAsync(ms);
            var bytes = ms.ToArray();
            Assert.True(bytes.Length > 50);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes[..4]));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Receipt_EnqueueEmail_WhenEnabled()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var (payments, receipts, root) = Create(fx);
        try
        {
            var payment = await payments.CreateAsync(new CreatePaymentDto
            {
                HorizontalPropertyId = seed.Property.Id,
                UnitId = seed.Unit.Id,
                PersonId = seed.Person.Id,
                Amount = 100_000m,
                PaidAt = DateTime.UtcNow,
                Method = PaymentMethod.Transfer,
                Reference = "RC-2",
                Allocations = [new CreatePaymentAllocationDto { FeeChargeId = seed.Charge.Id, Amount = 100_000m }],
            });

            await receipts.EnsureReceiptAsync(payment.Id, enqueueEmailIfEnabled: true);
            var msg = await fx.Db.OutboxMessages.SingleAsync(m => m.MessageType == OutboxMessageTypes.EmailSend);
            Assert.Contains("ana@example.com", msg.PayloadJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("attachmentFileAssetId", msg.PayloadJson, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
