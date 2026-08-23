using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Property.Api.Authorization;
using Property.Api.Controllers;
using Property.Application.Authorization;
using Property.Application.Features.Organization.UserPersonLinks;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Authorization;
using Property.Infrastructure.Identity;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;
using DomainUnit = Property.Domain.Entities.Unit;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class PortalServiceTests
{
    private static async Task<(User user, Person person, DomainUnit unit, DomainUnit otherUnit)> SeedLinkedAsync(
        TenantDbFixture fx)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "residente@example.com",
            NormalizedEmail = "RESIDENTE@EXAMPLE.COM",
            PasswordHash = "x",
        };
        var person = new Person
        {
            Id = Guid.NewGuid(),
            DocumentType = DocumentType.Cc,
            DocumentNumber = "100",
            FirstName = "Luis",
            LastName = "García",
            IsActive = true,
        };
        var property = new HorizontalProperty
        {
            Id = Guid.NewGuid(),
            Name = "Conjunto Portal",
            AddressLine = "Calle 1",
            City = "Pasto",
            IsActive = true,
        };
        var unit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "301",
            UnitType = UnitType.Apartment,
            Coefficient = 1,
            IsActive = true,
        };
        var otherUnit = new DomainUnit
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = property.Id,
            Code = "999",
            UnitType = UnitType.Apartment,
            Coefficient = 1,
            IsActive = true,
        };

        fx.Db.Users.Add(user);
        fx.Db.People.Add(person);
        fx.Db.TenantUsers.Add(new TenantUser { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true });
        fx.Db.HorizontalProperties.Add(property);
        fx.Db.Units.AddRange(unit, otherUnit);
        fx.Db.UnitOccupancies.Add(new UnitOccupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            PersonId = person.Id,
            Role = OccupancyRole.Owner,
            IsPrimary = true,
            IsActive = true,
            StartDate = new DateOnly(2024, 1, 1),
        });
        await fx.Db.SaveChangesAsync();

        var links = new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext);
        await links.LinkAsync(person.Id, new LinkUserToPersonDto { UserId = user.Id });
        return (user, person, unit, otherUnit);
    }

    [Fact]
    public async Task PortalAuth_IssuesUnitClaims()
    {
        using var fx = new TenantDbFixture();
        var (user, person, unit, _) = await SeedLinkedAsync(fx);
        var links = new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext);
        var (personId, unitIds) = await links.GetPortalScopeAsync(user.Id);

        Assert.Equal(person.Id, personId);
        Assert.Contains(unit.Id, unitIds);

        var jwt = new JwtTokenService(
            Microsoft.Extensions.Options.Options.Create(new JwtSettings
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
            [SystemRoles.Owner],
            [FeatureCodes.PortalAccess],
            personId,
            unitIds);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);
        Assert.Equal(person.Id.ToString(), parsed.Claims.First(c => c.Type == PortalClaims.PersonId).Value);
        Assert.Contains(parsed.Claims, c => c.Type == PortalClaims.UnitId && c.Value == unit.Id.ToString());
    }

    [Fact]
    public async Task PortalApi_ScopesToLinkedUnits()
    {
        using var fx = new TenantDbFixture();
        var (user, _, unit, otherUnit) = await SeedLinkedAsync(fx);
        var sut = new PortalService(fx.Db, new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext));

        var home = await sut.GetHomeAsync(user.Id);

        Assert.Single(home.Units);
        Assert.Equal(unit.Id, home.Units[0].UnitId);
        Assert.DoesNotContain(home.Units, u => u.UnitId == otherUnit.Id);
        Assert.True(await sut.IsUnitInScopeAsync(user.Id, unit.Id));
        Assert.False(await sut.IsUnitInScopeAsync(user.Id, otherUnit.Id));
    }

    [Fact]
    public async Task PortalAccount_ReturnsOnlyLinkedUnits()
    {
        using var fx = new TenantDbFixture();
        var (user, person, unit, otherUnit) = await SeedLinkedAsync(fx);

        var period = new BillingPeriod
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = (await fx.Db.HorizontalProperties.FirstAsync()).Id,
            Year = 2026,
            Month = 8,
            Status = BillingPeriodStatus.Open,
        };
        fx.Db.BillingPeriods.Add(period);
        fx.Db.FeeCharges.AddRange(
            new FeeCharge
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = period.HorizontalPropertyId,
                BillingPeriodId = period.Id,
                UnitId = unit.Id,
                ChargeType = FeeChargeType.Ordinary,
                Description = "Cuota linked",
                CoefficientSnapshot = 1m,
                Amount = 100_000m,
                AmountPaid = 0m,
                DueDate = new DateOnly(2026, 8, 10),
                Status = FeeChargeStatus.Pending,
                IsActive = true,
            },
            new FeeCharge
            {
                Id = Guid.NewGuid(),
                HorizontalPropertyId = period.HorizontalPropertyId,
                BillingPeriodId = period.Id,
                UnitId = otherUnit.Id,
                ChargeType = FeeChargeType.Ordinary,
                Description = "Cuota other",
                CoefficientSnapshot = 1m,
                Amount = 999_000m,
                AmountPaid = 0m,
                DueDate = new DateOnly(2026, 8, 10),
                Status = FeeChargeStatus.Pending,
                IsActive = true,
            });
        fx.Db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = period.HorizontalPropertyId,
            UnitId = otherUnit.Id,
            PersonId = person.Id,
            Amount = 50_000m,
            PaidAt = DateTime.UtcNow.AddDays(-1),
            Method = PaymentMethod.Cash,
            IsActive = true,
        });
        await fx.Db.SaveChangesAsync();

        var sut = new PortalService(fx.Db, new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext));
        var account = await sut.GetAccountAsync(user.Id);

        Assert.Single(account.Units);
        Assert.Equal(unit.Id, account.Units[0].UnitId);
        Assert.Equal(100_000m, account.OpenBalance);
        Assert.DoesNotContain(account.Units, u => u.UnitId == otherUnit.Id);
        Assert.Empty(account.RecentPayments);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.GetAccountUnitAsync(user.Id, otherUnit.Id));

        var detail = await sut.GetAccountUnitAsync(user.Id, unit.Id);
        Assert.Equal(unit.Id, detail.UnitId);
        Assert.Single(detail.OpenCharges);
        Assert.Equal(100_000m, detail.OpenBalance);
    }

    [Fact]
    public async Task PortalAccount_Forbidden_WithoutPersonLink()
    {
        using var fx = new TenantDbFixture();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "nolink@example.com",
            NormalizedEmail = "NOLINK@EXAMPLE.COM",
            PasswordHash = "x",
        };
        fx.Db.Users.Add(user);
        fx.Db.TenantUsers.Add(new TenantUser { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true });
        await fx.Db.SaveChangesAsync();

        var sut = new PortalService(fx.Db, new UserPersonLinkService(fx.UnitOfWork, fx.Db, fx.TenantContext));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetAccountAsync(user.Id));
    }

    [Fact]
    public void Portal_Forbidden_WhenModuleOff()
    {
        var attr = typeof(PortalController).GetCustomAttribute<RequireTenantModuleAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(TenantModuleKeys.Portal, attr!.ModuleKey);

        var feature = typeof(PortalController).GetCustomAttribute<RequireFeatureAttribute>();
        Assert.NotNull(feature);
        Assert.Equal(FeatureAuthorizationPolicies.For(FeatureCodes.PortalAccess), feature!.Policy);
    }
}
