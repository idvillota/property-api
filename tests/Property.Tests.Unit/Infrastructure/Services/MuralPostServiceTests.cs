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
            fx.Repository<HorizontalProperty>(),
            fx.Repository<DomainUnit>(),
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
}
