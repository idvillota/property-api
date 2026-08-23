using Microsoft.EntityFrameworkCore;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class UnitHouseholdBalanceTests
{
    [Fact]
    public async Task UnitSummary_IncludesBalance()
    {
        using var fx = new TenantDbFixture();
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
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
            Month = 1,
            Status = BillingPeriodStatus.Open,
        };
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.Add(new FeeCharge
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            BillingPeriodId = period.Id,
            UnitId = unit.Id,
            ChargeType = FeeChargeType.Ordinary,
            Description = "Cuota",
            CoefficientSnapshot = 1m,
            Amount = 200_000m,
            AmountPaid = 50_000m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            Status = FeeChargeStatus.Partial,
            IsActive = true,
        });
        await fx.Db.SaveChangesAsync();

        var sut = new UnitService(
            fx.Repository<DomainUnit>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<Building>(),
            fx.Repository<Floor>(),
            fx.Repository<UnitOccupancy>(),
            fx.Repository<Person>(),
            fx.Repository<Vehicle>(),
            fx.Repository<FeeCharge>(),
            fx.UnitOfWork);

        var household = await sut.GetHouseholdAsync(unit.Id);
        Assert.NotNull(household);
        Assert.Equal(150_000m, household!.OpenBalance);
        Assert.Equal(150_000m, household.OverdueBalance);
    }
}
