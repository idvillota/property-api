using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Property.Api.Controllers;
using Property.Application.Auditing;
using Property.Application.Authorization;
using Property.Application.Features.Organization.RolePermissions;
using Property.Domain.Entities;
using Property.Infrastructure.Auditing;
using Property.Infrastructure.Authorization;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Auditing;

public sealed class AuditWriterTests
{
    private static async Task SeedFeaturesAndRoleAsync(TenantDbFixture fx)
    {
        foreach (var def in FeatureCatalog.All)
        {
            fx.Db.Features.Add(new Feature
            {
                Id = def.Id,
                Code = def.Code,
                Name = def.Name,
                Module = def.Module,
                SortOrder = def.SortOrder,
            });
        }

        fx.Db.Roles.Add(new Role
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = SystemRoles.Manager,
            NormalizedName = SystemRoles.Manager.ToUpperInvariant(),
        });
        await fx.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task AuditWriter_OnRolePermissionChange()
    {
        using var fx = new TenantDbFixture();
        await SeedFeaturesAndRoleAsync(fx);
        fx.TenantContext.UserId = Guid.NewGuid();

        var roleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var sut = new RolePermissionService(
            fx.Db,
            fx.TenantContext,
            new AuditWriter(fx.Db, fx.TenantContext));

        await sut.UpdateRolePermissionsAsync(
            roleId,
            new UpdateRolePermissionsDto
            {
                FeatureCodes = [FeatureCodes.DashboardView, FeatureCodes.ReceptionManage],
            });

        var evt = await fx.Db.AuditEvents.SingleAsync();
        Assert.Equal(AuditActions.RolePermissionsUpdated, evt.Action);
        Assert.Equal(AuditEntityTypes.Role, evt.EntityType);
        Assert.Equal(roleId.ToString(), evt.EntityId);
        Assert.Equal(fx.TenantContext.UserId, evt.ActorUserId);
        Assert.NotNull(evt.BeforeJson);
        Assert.NotNull(evt.AfterJson);
        Assert.Contains(FeatureCodes.DashboardView, evt.AfterJson);
        Assert.Contains(FeatureCodes.ReceptionManage, evt.AfterJson);
    }

    [Fact]
    public async Task Audit_Written_OnRoleChange()
    {
        // Spec ORG-010 alias of AuditWriter_OnRolePermissionChange
        await AuditWriter_OnRolePermissionChange();
    }

    [Fact]
    public void AuditEvent_NoHardDeleteApi()
    {
        var controller = typeof(AuditEventsController);
        var httpMethods = controller
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes())
            .ToList();

        Assert.DoesNotContain(httpMethods, a => a is HttpDeleteAttribute);
        Assert.DoesNotContain(httpMethods, a => a is HttpPutAttribute);
        Assert.DoesNotContain(httpMethods, a => a is HttpPatchAttribute);
        Assert.Contains(httpMethods, a => a is HttpGetAttribute);

        // No public repository delete surface for AuditEvent on API controllers.
        var deleteRoutes = typeof(AuditEventsController).Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.Name.Contains("Audit", StringComparison.OrdinalIgnoreCase))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<HttpDeleteAttribute>() is not null)
            .ToList();

        Assert.Empty(deleteRoutes);
    }

    [Fact]
    public void Audit_NoDeleteEndpoint()
    {
        // Spec ORG-010 alias
        AuditEvent_NoHardDeleteApi();
    }
}
