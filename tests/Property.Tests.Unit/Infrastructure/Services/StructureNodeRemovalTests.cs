using Microsoft.EntityFrameworkCore;
using Property.Application.Features.Structure.Composer;
using Property.Domain.Common;
using Property.Domain.Entities;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class StructureNodeRemovalTests
{
    [Fact]
    public async Task RemoveFloor_RetiresItsUnits_AndRedistributesTheRest()
    {
        using var fx = new TenantDbFixture();
        var propertyId = await SeedTwoFloorsAsync(fx);
        var removal = CreateSut(fx);

        var floor = await fx.Db.Floors.OrderBy(f => f.Number).FirstAsync();
        var removed = await removal.RemoveFloorAsync(floor.Id);

        Assert.True(removed);
        Assert.False(await fx.Db.Floors.AnyAsync(f => f.Id == floor.Id && f.IsActive));
        Assert.Equal(0, await fx.Db.Units.CountAsync(u => u.FloorId == floor.Id && u.IsActive));
        Assert.Equal(2, await fx.Db.Units.CountAsync(u => u.IsActive));

        var remaining = await fx.Db.Units.Where(u => u.IsActive).SumAsync(u => u.Coefficient);
        Assert.Equal(CoefficientRules.Total, remaining);
        Assert.NotEqual(Guid.Empty, propertyId);
    }

    [Fact]
    public async Task RemoveBuilding_RetiresFloorsAndUnits()
    {
        using var fx = new TenantDbFixture();
        await SeedTwoFloorsAsync(fx);
        var removal = CreateSut(fx);

        var building = await fx.Db.Buildings.SingleAsync();
        var removed = await removal.RemoveBuildingAsync(building.Id);

        Assert.True(removed);
        Assert.False(await fx.Db.Buildings.AnyAsync(b => b.IsActive));
        Assert.Equal(0, await fx.Db.Floors.CountAsync(f => f.IsActive));
        Assert.Equal(0, await fx.Db.Units.CountAsync(u => u.IsActive));
    }

    private static StructureNodeRemoval CreateSut(TenantDbFixture fx) =>
        new(
            fx.Repository<Building>(),
            fx.Repository<Floor>(),
            fx.Repository<DomainUnit>(),
            fx.UnitOfWork);

    private static async Task<Guid> SeedTwoFloorsAsync(TenantDbFixture fx)
    {
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "PH",
            AddressLine = "1",
            City = "Pasto",
            Country = "CO",
            IsActive = true,
        };
        fx.Db.HorizontalProperties.Add(property);
        await fx.Db.SaveChangesAsync();

        var composer = new StructureComposerService(fx.Db);
        await composer.CommitAsync(new StructureComposerRequestDto
        {
            HorizontalPropertyId = property.Id,
            Buildings =
            [
                new StructureComposerBuildingDto
                {
                    Name = "Torre A",
                    Code = "A",
                    FloorFrom = 1,
                    FloorTo = 2,
                    UnitsPerFloor = 2,
                    DefaultAreaM2 = 70,
                },
            ],
        });

        return property.Id;
    }
}
