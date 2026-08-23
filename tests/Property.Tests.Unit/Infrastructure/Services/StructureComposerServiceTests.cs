using Microsoft.EntityFrameworkCore;
using Property.Application.Features.Structure.Composer;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class StructureComposerServiceTests
{
    private static StructureComposerService CreateSut(TenantDbFixture fx) => new(fx.Db);

    [Fact]
    public async Task StructureComposer_Commit_CreatesGraph()
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
        fx.Db.HorizontalProperties.Add(property);
        await fx.Db.SaveChangesAsync();

        var sut = CreateSut(fx);
        var result = await sut.CommitAsync(new StructureComposerRequestDto
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

        Assert.Equal(1, result.BuildingsCreated);
        Assert.True(result.FloorsCreated >= 2);
        Assert.Equal(4, result.UnitsCreated);
        Assert.Equal(1, await fx.Db.Buildings.CountAsync());
        Assert.Equal(4, await fx.Db.Units.CountAsync(u => u.IsActive));
    }

    [Fact]
    public async Task StructureComposer_Commit_BlocksZeroUnits()
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
        fx.Db.HorizontalProperties.Add(property);
        await fx.Db.SaveChangesAsync();

        var sut = CreateSut(fx);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CommitAsync(new StructureComposerRequestDto
            {
                HorizontalPropertyId = property.Id,
                Buildings =
                [
                    new StructureComposerBuildingDto
                    {
                        Name = "Torre A",
                        Code = "A",
                        FloorFrom = 1,
                        FloorTo = 1,
                        UnitsPerFloor = 2,
                        DefaultAreaM2 = 70,
                    },
                ],
                UnitOverrides =
                [
                    new StructureComposerUnitOverrideDto { Code = "101", Remove = true },
                    new StructureComposerUnitOverrideDto { Code = "102", Remove = true },
                ],
            }));

        Assert.Contains("at least one unit", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
