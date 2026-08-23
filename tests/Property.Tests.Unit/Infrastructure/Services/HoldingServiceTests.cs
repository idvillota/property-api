using Microsoft.EntityFrameworkCore;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class HoldingServiceTests
{
    [Fact]
    public async Task Holding_Aggregates_OnlyAuthorizedProperties()
    {
        using var fxA = new TenantDbFixture();
        using var fxB = new TenantDbFixture();

        var propertyA1 = await SeedPropertyAsync(fxA, "Torre Norte", units: 2, occupied: 1, openBalance: 100_000m, overdueBalance: 50_000m);
        var propertyA2 = await SeedPropertyAsync(fxA, "Torre Sur", units: 3, occupied: 2, openBalance: 200_000m, overdueBalance: 80_000m);
        _ = await SeedPropertyAsync(fxB, "Otro Tenant", units: 5, occupied: 4, openBalance: 999_000m, overdueBalance: 500_000m);

        var sut = new HoldingService(fxA.Db);
        var summary = await sut.GetSummaryAsync();

        Assert.Equal(2, summary.Properties.Count);
        Assert.All(summary.Properties, p => Assert.NotEqual(Guid.Empty, p.PropertyId));

        var north = Assert.Single(summary.Properties, p => p.PropertyId == propertyA1);
        Assert.Equal("Torre Norte", north.Name);
        Assert.Equal(100_000m, north.OpenBalance);
        Assert.Equal(50_000m, north.OverdueBalance);
        Assert.Equal(1, north.OccupiedUnits);
        Assert.Equal(2, north.TotalUnits);

        var south = Assert.Single(summary.Properties, p => p.PropertyId == propertyA2);
        Assert.Equal("Torre Sur", south.Name);
        Assert.Equal(200_000m, south.OpenBalance);
        Assert.Equal(80_000m, south.OverdueBalance);
        Assert.Equal(2, south.OccupiedUnits);
        Assert.Equal(3, south.TotalUnits);

        Assert.DoesNotContain(summary.Properties, p => p.Name == "Otro Tenant");
    }

    private static async Task<Guid> SeedPropertyAsync(
        TenantDbFixture fx,
        string name,
        int units,
        int occupied,
        decimal openBalance,
        decimal overdueBalance)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = name,
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);

        var unitEntities = new List<DomainUnit>();
        for (var i = 0; i < units; i++)
        {
            var unit = new DomainUnit
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = property.Id,
                Code = $"{100 + i}",
                UnitType = UnitType.Apartment,
                Coefficient = 1m,
                IsActive = true,
                IsBillable = true,
            };
            unitEntities.Add(unit);
            fx.Db.Units.Add(unit);
        }

        for (var i = 0; i < occupied && i < unitEntities.Count; i++)
        {
            var person = new Person
            {
                Id = Guid.NewGuid(),
                DocumentType = DocumentType.Cc,
                DocumentNumber = $"{name}-{i}",
                FirstName = "Occ",
                LastName = $"{i}",
                IsActive = true,
            };
            fx.Db.People.Add(person);
            fx.Db.UnitOccupancies.Add(new UnitOccupancy
            {
                Id = Guid.NewGuid(),
                UnitId = unitEntities[i].Id,
                PersonId = person.Id,
                Role = OccupancyRole.Owner,
                IsPrimary = true,
                IsActive = true,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            });
        }

        if (openBalance > 0 && unitEntities.Count > 0)
        {
            var period = new BillingPeriod
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = property.Id,
                Year = 2026,
                Month = 6,
                Status = BillingPeriodStatus.Open,
            };
            fx.Db.BillingPeriods.Add(period);

            if (overdueBalance > 0)
            {
                fx.Db.FeeCharges.Add(new FeeCharge
                {
                    Id = Guid.NewGuid(),
                    HorizontalPropertyId = property.Id,
                    BillingPeriodId = period.Id,
                    UnitId = unitEntities[0].Id,
                    ChargeType = FeeChargeType.Ordinary,
                    Description = "Cuota vencida",
                    CoefficientSnapshot = 1m,
                    Amount = overdueBalance,
                    AmountPaid = 0m,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-15),
                    Status = FeeChargeStatus.Pending,
                    IsActive = true,
                });
            }

            var currentBalance = openBalance - overdueBalance;
            if (currentBalance > 0)
            {
                var unitId = unitEntities.Count > 1 ? unitEntities[1].Id : unitEntities[0].Id;
                fx.Db.FeeCharges.Add(new FeeCharge
                {
                    Id = Guid.NewGuid(),
                    HorizontalPropertyId = property.Id,
                    BillingPeriodId = period.Id,
                    UnitId = unitId,
                    ChargeType = FeeChargeType.Ordinary,
                    Description = "Cuota corriente",
                    CoefficientSnapshot = 1m,
                    Amount = currentBalance,
                    AmountPaid = 0m,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(15),
                    Status = FeeChargeStatus.Pending,
                    IsActive = true,
                });
            }
        }

        await fx.Db.SaveChangesAsync();
        return property.Id;
    }
}
