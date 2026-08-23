using Microsoft.EntityFrameworkCore;
using Property.Application.Features.Billing.PaymentIntents;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class PaymentIntentServiceTests
{
    private sealed record Seed(
        HorizontalProperty Property,
        DomainUnit Unit,
        FeeCharge Charge);

    private static async Task<Seed> SeedBillingAsync(TenantDbFixture fx, decimal chargeAmount = 100m)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH Demo",
            AddressLine = "Calle 1",
            City = "Pasto",
            Country = "Colombia",
            IsActive = true,
        };
        var unit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "101",
            UnitType = UnitType.Apartment,
            Coefficient = 1m,
            IsActive = true,
            IsBillable = true,
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
            Amount = chargeAmount,
            AmountPaid = 0m,
            DueDate = new DateOnly(2026, 8, 30),
            Status = FeeChargeStatus.Pending,
            IsActive = true,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(charge);
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unit, charge);
    }

    private static PaymentIntentService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            new PaymentService(
                fx.Db,
                fx.Repository<Payment>(),
                fx.Repository<FeeCharge>(),
                fx.Repository<DomainUnit>(),
                fx.Repository<HorizontalProperty>(),
                fx.Repository<Person>(),
                fx.UnitOfWork,
                new Property.Infrastructure.Auditing.AuditWriter(fx.Db, fx.TenantContext),
                new Property.Infrastructure.Messaging.OutboxWriter(
                    fx.Db,
                    fx.TenantContext,
                    Microsoft.Extensions.Options.Options.Create(
                        new Property.Application.Common.Options.OutboxOptions()))),
            new Property.Application.Common.Interfaces.NullPaymentReceiptService(),
            fx.UnitOfWork,
            new Property.Infrastructure.Messaging.OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(
                    new Property.Application.Common.Options.OutboxOptions())));

    private static CreatePaymentIntentDto CreateDto(Seed seed, string key, decimal amount = 100m) =>
        new()
        {
            IdempotencyKey = key,
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            Amount = amount,
            Allocations =
            [
                new CreatePaymentIntentAllocationDto
                {
                    FeeChargeId = seed.Charge.Id,
                    Amount = amount,
                },
            ],
        };

    [Fact]
    public async Task PaymentIntent_Create_IsIdempotent()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx);
        var sut = CreateSut(fx);
        var dto = CreateDto(seed, "key-abc");

        var first = await sut.CreateOrGetAsync(dto);
        var second = await sut.CreateOrGetAsync(dto);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("key-abc", second.IdempotencyKey);
        Assert.Equal(PaymentIntentStatus.Pending, second.Status);
        Assert.Equal(1, await fx.Db.PaymentIntents.CountAsync());
    }

    [Fact]
    public async Task PaymentIntent_Paid_CreatesSinglePayment()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx);
        var sut = CreateSut(fx);

        var intent = await sut.CreateOrGetAsync(CreateDto(seed, "pay-once"));
        var paid1 = await sut.MarkPaidAsync(intent.Id, new MarkPaymentIntentPaidDto { ProviderRef = "psp-1" });
        var paid2 = await sut.MarkPaidAsync(intent.Id, new MarkPaymentIntentPaidDto { ProviderRef = "psp-1" });

        Assert.Equal(PaymentIntentStatus.Paid, paid1.Status);
        Assert.Equal(PaymentIntentStatus.Paid, paid2.Status);
        Assert.Equal(paid1.PaymentId, paid2.PaymentId);
        Assert.NotNull(paid1.PaymentId);
        Assert.Equal(1, await fx.Db.Payments.CountAsync(p => p.IsActive));
        Assert.Equal(1, await fx.Db.PaymentAllocations.CountAsync());
    }

    [Fact]
    public async Task PaymentIntent_Failed_HasNoPayment()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx);
        var sut = CreateSut(fx);

        var intent = await sut.CreateOrGetAsync(CreateDto(seed, "fail-key"));
        var failed = await sut.MarkFailedAsync(intent.Id, new MarkPaymentIntentFailedDto { Reason = "declined" });

        Assert.Equal(PaymentIntentStatus.Failed, failed.Status);
        Assert.Null(failed.PaymentId);
        Assert.Equal(0, await fx.Db.Payments.CountAsync());
    }
}
