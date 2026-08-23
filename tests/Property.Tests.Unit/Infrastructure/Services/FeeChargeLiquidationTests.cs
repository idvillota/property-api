using Microsoft.EntityFrameworkCore;
using Property.Application.Features.Billing.FeeCharges;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class FeeChargeLiquidationTests
{
    private sealed record Seed(
        HorizontalProperty Property,
        BillingPeriod Period,
        DomainUnit UnitA,
        DomainUnit UnitB,
        DomainUnit NonBillable);

    private static async Task<Seed> SeedAsync(TenantDbFixture fx, decimal budget = 1_000_000m)
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
        var period = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Year = 2026,
            Month = 8,
            Status = BillingPeriodStatus.Open,
            IsLiquidated = false,
        };
        var unitA = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "101",
            UnitType = UnitType.Apartment,
            Coefficient = 40m,
            IsActive = true,
            IsBillable = true,
        };
        var unitB = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "102",
            UnitType = UnitType.Apartment,
            Coefficient = 60m,
            IsActive = true,
            IsBillable = true,
        };
        var nonBillable = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "P-1",
            UnitType = UnitType.Parking,
            Coefficient = 0m,
            IsActive = true,
            IsBillable = false,
        };
        var settings = new FeeSettings
        {
            HorizontalPropertyId = property.Id,
            MonthlyBudgetAmount = budget,
            DueDayOfMonth = 10,
            LateInterestMonthlyPercent = 0m,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.Units.AddRange(unitA, unitB, nonBillable);
        fx.Db.FeeSettings.Add(settings);
        await fx.Db.SaveChangesAsync();
        return new Seed(property, period, unitA, unitB, nonBillable);
    }

    private static FeeChargeService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Db,
            fx.Repository<FeeCharge>(),
            fx.Repository<BillingPeriod>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.UnitOfWork);

    private static LiquidationCommitDto Dto(Seed seed) =>
        new()
        {
            HorizontalPropertyId = seed.Property.Id,
            BillingPeriodId = seed.Period.Id,
        };

    [Fact]
    public async Task Liquidation_CreatesCharges_ForBillableUnits()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var result = await sut.CommitLiquidationAsync(Dto(seed));

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(1_000_000m, result.TotalAmount);
        Assert.True(result.IsLiquidated);
        Assert.Equal(2, await fx.Db.FeeCharges.CountAsync(c => c.IsActive));
        Assert.DoesNotContain(
            await fx.Db.FeeCharges.ToListAsync(),
            c => c.UnitId == seed.NonBillable.Id);

        var period = await fx.Db.BillingPeriods.SingleAsync(p => p.Id == seed.Period.Id);
        Assert.True(period.IsLiquidated);
        Assert.NotNull(period.LiquidatedAtUtc);
    }

    [Fact]
    public async Task Liquidation_Rejects_WhenPeriodAlreadyLiquidated()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        await sut.CommitLiquidationAsync(Dto(seed));
        var before = await fx.Db.FeeCharges.CountAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CommitLiquidationAsync(Dto(seed)));
        Assert.Contains("already liquidated", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, await fx.Db.FeeCharges.CountAsync());
    }

    [Fact]
    public async Task Liquidation_Preview_DoesNotPersist()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var preview = await sut.PreviewLiquidationAsync(Dto(seed));

        Assert.Equal(2, preview.BillableUnitCount);
        Assert.Equal(2, preview.ChargeCount);
        Assert.Equal(1_000_000m, preview.TotalAmount);
        Assert.False(preview.IsLiquidated);
        Assert.Equal(0, await fx.Db.FeeCharges.CountAsync());

        var period = await fx.Db.BillingPeriods.SingleAsync(p => p.Id == seed.Period.Id);
        Assert.False(period.IsLiquidated);
    }
}
