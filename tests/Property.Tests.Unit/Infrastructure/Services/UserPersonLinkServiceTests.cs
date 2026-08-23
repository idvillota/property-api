using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Property.Application.Authorization;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Identity;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class UserPersonLinkServiceTests
{
    private static async Task<(User user, Person person)> SeedUserAndPersonAsync(TenantDbFixture fx)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "portal@example.com",
            NormalizedEmail = "PORTAL@EXAMPLE.COM",
            PasswordHash = "x",
        };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "123",
            FirstName = "Ana",
            LastName = "Pérez",
            IsActive = true,
        };
        var membership = new TenantUser
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IsActive = true,
        };
        fx.Db.Users.Add(user);
        fx.Db.People.Add(person);
        fx.Db.TenantUsers.Add(membership);
        await fx.Db.SaveChangesAsync();
        return (user, person);
    }

    [Fact]
    public async Task UserPersonLink_Unique_PerTenantUser()
    {
        using var fx = new TenantDbFixture();
        var (user, person) = await SeedUserAndPersonAsync(fx);
        var otherPerson = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "999",
            FirstName = "Other",
            LastName = "Person",
            IsActive = true,
        };
        fx.Db.People.Add(otherPerson);
        await fx.Db.SaveChangesAsync();

        var sut = new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext);
        await sut.LinkAsync(person.Id, new LinkUserToPersonDto
        {
            UserId = user.Id,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.LinkAsync(otherPerson.Id, new LinkUserToPersonDto
            {
                UserId = user.Id,
            }));
    }

    [Fact]
    public async Task PortalToken_IncludesUnitClaims_FromOccupancies()
    {
        using var fx = new TenantDbFixture();
        var (user, person) = await SeedUserAndPersonAsync(fx);
        var unitId = Guid.NewGuid();
        fx.Db.UnitOccupancies.Add(new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            PersonId = person.Id,
            Role = OccupancyRole.Owner,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            IsPrimary = true,
        });
        await fx.Db.SaveChangesAsync();

        var linkService = new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext);
        await linkService.LinkAsync(person.Id, new LinkUserToPersonDto
        {
            UserId = user.Id,
        });

        var (personId, unitIds) = await linkService.GetPortalScopeAsync(user.Id);
        Assert.Equal(person.Id, personId);
        Assert.Contains(unitId, unitIds);

        var jwt = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtSettings
        {
            Issuer = "test",
            Audience = "test",
            SigningKey = "0123456789abcdef0123456789abcdef",
            AccessTokenMinutes = 60,
        }));
        var token = jwt.CreateAccessToken(
            user.Id,
            fx.TenantId,
            user.Email,
            ["Owner"],
            ["people.manage"],
            personId,
            unitIds);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        Assert.Equal(person.Id.ToString(), parsed.Claims.First(c => c.Type == PortalClaims.PersonId).Value);
        Assert.Contains(parsed.Claims, c => c.Type == PortalClaims.UnitId && c.Value == unitId.ToString());
    }

    [Fact]
    public async Task PortalApi_Forbidden_WithoutLink()
    {
        using var fx = new TenantDbFixture();
        var (user, _) = await SeedUserAndPersonAsync(fx);
        var sut = new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetPortalMeAsync(user.Id));

        var (personId, unitIds) = await sut.GetPortalScopeAsync(user.Id);
        Assert.Null(personId);
        Assert.Empty(unitIds);
    }
}
