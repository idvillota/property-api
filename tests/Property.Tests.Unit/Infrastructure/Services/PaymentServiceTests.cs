using Microsoft.EntityFrameworkCore;
using Property.Application.Features.Billing.Payments;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class PaymentServiceTests
{
    private sealed record Seed(
        HorizontalProperty Property,
        DomainUnit Unit,
        FeeCharge Charge);

    private static async Task<Seed> SeedBillingAsync(
        TenantDbFixture fx,
        decimal chargeAmount = 100_000m,
        decimal amountPaid = 0m)
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
            Description = "Cuota ordinaria",
            CoefficientSnapshot = 1m,
            Amount = chargeAmount,
            AmountPaid = amountPaid,
            DueDate = new DateOnly(2026, 8, 30),
            Status = amountPaid <= 0m
                ? FeeChargeStatus.Pending
                : amountPaid >= chargeAmount
                    ? FeeChargeStatus.Paid
                    : FeeChargeStatus.Partial,
            IsActive = true,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(charge);
        await fx.Db.SaveChangesAsync();
        return new Seed(property, unit, charge);
    }

    private static PaymentService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.Repository<Payment>(),
            fx.Repository<FeeCharge>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<Person>(),
            fx.UnitOfWork,
            new AuditWriter(fx.Db, fx.TenantContext),
            new Property.Infrastructure.Messaging.OutboxWriter(
                fx.Db,
                fx.TenantContext,
                Microsoft.Extensions.Options.Options.Create(
                    new Property.Application.Common.Options.OutboxOptions())));

    private static CreatePaymentDto CreateDto(
        Seed seed,
        decimal amount,
        decimal? allocationAmount = null,
        bool allowOverpayment = false) =>
        new()
        {
            HorizontalPropertyId = seed.Property.Id,
            UnitId = seed.Unit.Id,
            Amount = amount,
            PaidAt = DateTime.UtcNow,
            Method = PaymentMethod.Transfer,
            Reference = "REF-001",
            AllowOverpayment = allowOverpayment,
            Allocations =
            [
                new CreatePaymentAllocationDto
                {
                    FeeChargeId = seed.Charge.Id,
                    Amount = allocationAmount ?? amount,
                },
            ],
        };

    [Fact]
    public async Task PaymentService_Create_AssignsToOpenCharges()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx, chargeAmount: 100_000m);
        var sut = CreateSut(fx);

        var created = await sut.CreateAsync(CreateDto(seed, 100_000m));

        Assert.Equal(100_000m, created.Amount);
        Assert.Single(created.Allocations);
        Assert.Equal(0m, created.UnitOpenBalanceAfter);

        var charge = await fx.Db.FeeCharges.SingleAsync(c => c.Id == seed.Charge.Id);
        Assert.Equal(100_000m, charge.AmountPaid);
        Assert.Equal(FeeChargeStatus.Paid, charge.Status);
    }

    [Fact]
    public async Task PaymentService_SoftDelete_ReversesAllocations()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx, chargeAmount: 80_000m);
        var sut = CreateSut(fx);
        var created = await sut.CreateAsync(CreateDto(seed, 80_000m));

        var ok = await sut.SoftDeleteAsync(created.Id);

        Assert.True(ok);
        var payment = await fx.Db.Payments.SingleAsync(p => p.Id == created.Id);
        Assert.False(payment.IsActive);

        var charge = await fx.Db.FeeCharges.SingleAsync(c => c.Id == seed.Charge.Id);
        Assert.Equal(0m, charge.AmountPaid);
        Assert.Equal(FeeChargeStatus.Pending, charge.Status);
    }

    [Fact]
    public async Task PaymentService_RejectsOverpayment_WhenNotAllowed()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx, chargeAmount: 50_000m);
        var sut = CreateSut(fx);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateAsync(CreateDto(seed, amount: 60_000m, allocationAmount: 60_000m)));

        Assert.Contains("excede el saldo abierto", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await fx.Db.Payments.CountAsync());
        var charge = await fx.Db.FeeCharges.SingleAsync(c => c.Id == seed.Charge.Id);
        Assert.Equal(0m, charge.AmountPaid);
        Assert.Equal(FeeChargeStatus.Pending, charge.Status);
    }

    [Fact]
    public async Task PaymentService_AllowsOverpayment_WhenFlagged()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedBillingAsync(fx, chargeAmount: 50_000m);
        var sut = CreateSut(fx);

        var created = await sut.CreateAsync(
            CreateDto(seed, amount: 60_000m, allocationAmount: 50_000m, allowOverpayment: true));

        Assert.Equal(60_000m, created.Amount);
        Assert.Single(created.Allocations);
        Assert.Equal(50_000m, created.Allocations[0].Amount);
        Assert.Equal(0m, created.UnitOpenBalanceAfter);

        var charge = await fx.Db.FeeCharges.SingleAsync(c => c.Id == seed.Charge.Id);
        Assert.Equal(FeeChargeStatus.Paid, charge.Status);
    }
}
