using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;
using Property.Application.Features.Auth;
using Property.Domain.Entities;
using Property.Infrastructure.Authorization;
using Property.Infrastructure.Identity;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class AuthServiceTests
{
    private static async Task SeedFeaturesAsync(NoTenantDbFixture fx)
    {
        foreach (var def in FeatureCatalog.All)
        {
            await fx.Db.Features.AddAsync(new Feature
            {
                Id = def.Id,
                Code = def.Code,
                Name = def.Name,
                Module = def.Module,
                SortOrder = def.SortOrder,
            });
        }

        await fx.Db.SaveChangesAsync();
    }

    private static AuthService CreateSut(NoTenantDbFixture fx, Mock<IJwtTokenService>? jwt = null)
    {
        var useDefaultJwt = jwt is null;
        jwt ??= new Mock<IJwtTokenService>();
        if (useDefaultJwt)
        {
            jwt.Setup(j => j.CreateAccessToken(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<IReadOnlyList<Guid>>()))
                .Returns("test-token");
        }

        var tenantContext = new Property.Infrastructure.Common.CurrentTenantContext();
        IRolePermissionService rolePermissions = new RolePermissionService(
            fx.Db,
            tenantContext,
            new Property.Infrastructure.Auditing.AuditWriter(fx.Db, tenantContext));
        var platform = Options.Create(new PlatformOptions
        {
            AdminEmails = ["idvillota@gmail.com"],
        });
        var links = new Mock<IUserPersonLinkService>();
        links.Setup(l => l.GetPortalScopeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, (IReadOnlyList<Guid>)Array.Empty<Guid>()));
        var modules = new Mock<ITenantModuleService>();
        modules.Setup(m => m.GetFlagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Features.Organization.TenantSettings.TenantModuleFlagsDto());
        return new AuthService(
            fx.UnitOfWork,
            new BcryptPasswordHasher(),
            jwt.Object,
            rolePermissions,
            tenantContext,
            links.Object,
            modules.Object,
            platform);
    }

    [Fact]
    public async Task RegisterTenantAsync_persists_tenant_user_roles_and_membership()
    {
        using var fx = new NoTenantDbFixture();
        await SeedFeaturesAsync(fx);
        var sut = CreateSut(fx);

        var result = await sut.RegisterTenantAsync(
            new RegisterTenantDto
            {
                TenantName = "Central Properties",
                AdminEmail = "owner@example.com",
                Password = "Str0ng!Pass",
            });

        Assert.Equal("test-token", result.AccessToken);
        Assert.Equal("owner@example.com", result.Email);
        Assert.Single(await fx.Db.Tenants.ToListAsync());
        Assert.Single(await fx.Db.Users.ToListAsync());
        Assert.Equal(3, await fx.Db.Roles.CountAsync());
        Assert.Single(await fx.Db.TenantUsers.ToListAsync());
        Assert.NotNull(result.Modules);
        Assert.True(result.Modules.Portal);
    }

    [Fact]
    public async Task RegisterTenantAsync_throws_when_email_already_used()
    {
        using var fx = new NoTenantDbFixture();
        await SeedFeaturesAsync(fx);
        var sut = CreateSut(fx);
        var dto = new RegisterTenantDto
        {
            TenantName = "First",
            AdminEmail = "dup@example.com",
            Password = "Str0ng!Pass",
        };
        await sut.RegisterTenantAsync(dto);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RegisterTenantAsync(
                new RegisterTenantDto
                {
                    TenantName = "Second",
                    AdminEmail = "dup@example.com",
                    Password = "OtherPass1!",
                }));
    }

    [Fact]
    public async Task LoginAsync_after_register_returns_token()
    {
        using var fx = new NoTenantDbFixture();
        await SeedFeaturesAsync(fx);
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.CreateAccessToken(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyList<Guid>>()))
            .Returns("jwt-value");
        var sut = CreateSut(fx, jwt);
        await sut.RegisterTenantAsync(
            new RegisterTenantDto
            {
                TenantName = "Login Properties",
                AdminEmail = "admin@example.com",
                Password = "Secret123!",
            });

        var login = await sut.LoginAsync(
            new LoginDto { Email = "admin@example.com", Password = "Secret123!" });

        Assert.Equal("jwt-value", login.AccessToken);
        Assert.Equal("admin@example.com", login.Email);
    }

    [Fact]
    public async Task LoginAsync_without_slug_throws_when_multiple_tenants()
    {
        using var fx = new NoTenantDbFixture();
        await SeedFeaturesAsync(fx);
        var sut = CreateSut(fx);

        await sut.RegisterTenantAsync(
            new RegisterTenantDto
            {
                TenantName = "North Properties",
                AdminEmail = "multi@example.com",
                Password = "Secret123!",
            });

        var tenantB = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "South Properties",
            Slug = "south-properties",
            IsActive = true,
        };
        await fx.Db.Tenants.AddAsync(tenantB);

        var user = await fx.Db.Users.FirstAsync(u => u.Email == "multi@example.com");
        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB.Id,
            Name = SystemRoles.Manager,
            NormalizedName = SystemRoles.Manager.ToUpperInvariant(),
        };
        await fx.Db.Roles.AddAsync(role);
        var tenantUser = new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB.Id,
            UserId = user.Id,
            IsActive = true,
        };
        tenantUser.TenantUserRoles.Add(new TenantUserRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantB.Id,
            TenantUserId = tenantUser.Id,
            RoleId = role.Id,
        });
        await fx.Db.TenantUsers.AddAsync(tenantUser);
        await fx.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<MultipleTenantsLoginException>(() =>
            sut.LoginAsync(new LoginDto { Email = "multi@example.com", Password = "Secret123!" }));

        Assert.Equal(2, ex.Tenants.Count);
        Assert.Contains(ex.Tenants, t => t.Slug == "north-properties");
        Assert.Contains(ex.Tenants, t => t.Slug == "south-properties");
    }
}
