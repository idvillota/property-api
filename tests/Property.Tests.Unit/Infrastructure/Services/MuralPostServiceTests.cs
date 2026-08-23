using Microsoft.EntityFrameworkCore;
using Property.Application.Common.Models;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class MuralPostServiceTests
{
    private static MuralPostService CreateSut(TenantDbFixture fx) =>
        new(
            fx.Repository<MuralPost>(),
            fx.Repository<MuralPostRead>(),
            fx.Repository<HorizontalProperty>(),
            fx.Repository<DomainUnit>(),
            fx.Repository<Building>(),
            fx.Repository<Floor>(),
            fx.Repository<UnitOccupancy>(),
            fx.Repository<User>(),
            fx.TenantContext,
            fx.UnitOfWork);

    private static async Task<(HorizontalProperty Property, MuralPost Active, MuralPost Inactive)> SeedAsync(
        TenantDbFixture fx)
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
        var active = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Kind = MuralPostKind.Official,
            Category = MuralPostCategory.Other,
            Title = "Aviso activo",
            Body = "Contenido publicado",
            AuthorDisplayName = "Admin",
            IsPinned = false,
            IsHidden = false,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
        };
        var inactive = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Kind = MuralPostKind.Community,
            Category = MuralPostCategory.Other,
            Title = "Aviso inactivo",
            Body = "No debe verse",
            AuthorDisplayName = "Admin",
            IsPinned = false,
            IsHidden = false,
            PublishedAtUtc = DateTime.UtcNow.AddHours(-2),
            IsActive = false,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.MuralPosts.AddRange(active, inactive);
        await fx.Db.SaveChangesAsync();
        return (property, active, inactive);
    }

    [Fact]
    public async Task MuralPost_List_ExcludesInactive()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var sut = CreateSut(fx);

        var adminList = await sut.ListAsync(new ListQuery
        {
            Page = 1,
            PageSize = 50,
            IncludeInactive = false,
            Filters = new Dictionary<string, string>
            {
                ["horizontalPropertyId"] = seed.Property.Id.ToString(),
            },
        });

        Assert.Single(adminList.Items);
        Assert.Equal(seed.Active.Id, adminList.Items[0].Id);
        Assert.DoesNotContain(adminList.Items, p => p.Id == seed.Inactive.Id);

        var publicFeed = await sut.ListPublicAsync(seed.Property.Id);
        Assert.Single(publicFeed);
        Assert.Equal(seed.Active.Id, publicFeed[0].Id);
        Assert.DoesNotContain(publicFeed, p => !p.IsActive);
    }

    [Fact]
    public async Task Mural_SegmentFilter_ByBuilding()
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
        var tower1 = new Building
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Name = "Torre 1",
            IsActive = true,
        };
        var tower2 = new Building
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Name = "Torre 2",
            IsActive = true,
        };
        var forTower1 = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Kind = MuralPostKind.Official,
            Category = MuralPostCategory.Other,
            Title = "Solo Torre 1",
            Body = "Cortaremos agua en Torre 1",
            AuthorDisplayName = "Admin",
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
            AudienceBuildingIdsJson = System.Text.Json.JsonSerializer.Serialize(new[] { tower1.Id }),
            AudienceFloorIdsJson = "[]",
            AudienceOccupancyRolesJson = "[]",
        };
        var broadcast = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Kind = MuralPostKind.Official,
            Category = MuralPostCategory.Other,
            Title = "Todos",
            Body = "Aviso general",
            AuthorDisplayName = "Admin",
            PublishedAtUtc = DateTime.UtcNow.AddHours(-2),
            IsActive = true,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Buildings.AddRange(tower1, tower2);
        fx.Db.MuralPosts.AddRange(forTower1, broadcast);
        await fx.Db.SaveChangesAsync();

        var sut = CreateSut(fx);
        var tower2Reader = new Property.Application.Features.Community.Mural.MuralReaderScope
        {
            BuildingIds = new[] { tower2.Id },
            FloorIds = Array.Empty<Guid>(),
            OccupancyRoles = new[] { OccupancyRole.Resident },
        };

        var feed = await sut.ListPublicAsync(property.Id, tower2Reader);
        Assert.Single(feed);
        Assert.Equal(broadcast.Id, feed[0].Id);
        Assert.DoesNotContain(feed, p => p.Id == forTower1.Id);
    }

    [Fact]
    public async Task Mural_Segment_All_Visible()
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
        var tower1 = new Building
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Name = "Torre 1",
            IsActive = true,
        };
        var broadcast = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Kind = MuralPostKind.Official,
            Category = MuralPostCategory.Other,
            Title = "Para todos",
            Body = "Mantenimiento general",
            AuthorDisplayName = "Admin",
            PublishedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
            AudienceBuildingIdsJson = "[]",
            AudienceFloorIdsJson = "[]",
            AudienceOccupancyRolesJson = "[]",
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Buildings.Add(tower1);
        fx.Db.MuralPosts.Add(broadcast);
        await fx.Db.SaveChangesAsync();

        var sut = CreateSut(fx);
        var anyReader = new Property.Application.Features.Community.Mural.MuralReaderScope
        {
            BuildingIds = new[] { tower1.Id },
            FloorIds = Array.Empty<Guid>(),
            OccupancyRoles = new[] { OccupancyRole.Owner },
        };

        var feed = await sut.ListPublicAsync(property.Id, anyReader);
        Assert.Single(feed);
        Assert.Equal(broadcast.Id, feed[0].Id);
    }

    [Fact]
    public async Task MuralRead_Upsert_Once()
    {
        using var fx = new TenantDbFixture();
        var seed = await SeedAsync(fx);
        var personId = Guid.NewGuid();
        fx.Db.People.Add(new Person
        {
            Id = personId,
            DocumentType = DocumentType.Cc,
            DocumentNumber = "1",
            FirstName = "Ana",
            LastName = "R",
            IsActive = true,
        });
        await fx.Db.SaveChangesAsync();

        var sut = CreateSut(fx);
        var first = await sut.MarkReadAsync(seed.Active.Id, personId, Guid.NewGuid());
        var second = await sut.MarkReadAsync(seed.Active.Id, personId, Guid.NewGuid());

        Assert.False(first.AlreadyRead);
        Assert.True(second.AlreadyRead);
        Assert.Equal(first.ReadAtUtc, second.ReadAtUtc);
        Assert.Equal(1, await fx.Db.MuralPostReads.CountAsync(r => r.MuralPostId == seed.Active.Id));
    }

    [Fact]
    public async Task MuralRead_Stats_Percent()
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
            Coefficient = 1,
            IsActive = true,
        };
        var people = Enumerable.Range(0, 4).Select(i => new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = $"D{i}",
            FirstName = $"P{i}",
            LastName = "X",
            IsActive = true,
        }).ToList();
        var post = new MuralPost
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Kind = MuralPostKind.Official,
            Category = MuralPostCategory.Other,
            Title = "Acuse",
            Body = "Lee esto",
            AuthorDisplayName = "Admin",
            PublishedAtUtc = DateTime.UtcNow,
            IsActive = true,
        };

        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.Add(unit);
        fx.Db.People.AddRange(people);
        fx.Db.UnitOccupancies.AddRange(people.Select(p => new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            PersonId = p.Id,
            Role = OccupancyRole.Resident,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            IsActive = true,
        }));
        fx.Db.MuralPosts.Add(post);
        await fx.Db.SaveChangesAsync();

        var sut = CreateSut(fx);
        await sut.MarkReadAsync(post.Id, people[0].Id, null);
        await sut.MarkReadAsync(post.Id, people[1].Id, null);

        var stats = await sut.GetReadStatsAsync(post.Id);
        Assert.NotNull(stats);
        Assert.Equal(4, stats!.RecipientCount);
        Assert.Equal(2, stats.ReadCount);
        Assert.Equal(50m, stats.ReadPercent);
    }
}
