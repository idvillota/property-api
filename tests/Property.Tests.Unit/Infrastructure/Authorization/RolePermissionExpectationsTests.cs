using Property.Application.Authorization;
using Property.Infrastructure.Authorization;

namespace Property.Tests.Unit.Infrastructure.Authorization;

/// <summary>
/// Guards the default role matrix against regressions in feature assignments.
/// </summary>
public sealed class RolePermissionExpectationsTests
{
    [Theory]
    [InlineData(SystemRoles.Administrator)]
    [InlineData(SystemRoles.Manager)]
    public void Platform_roles_receive_all_features(string roleName)
    {
        var permissions = FeatureCatalog.DefaultFeaturesByRole[roleName];
        Assert.Equal(FeatureCodes.All.Count, permissions.Count);
        foreach (var code in FeatureCodes.All)
            Assert.Contains(code, permissions);
    }

    [Theory]
    [InlineData(SystemRoles.Owner)]
    [InlineData(SystemRoles.Resident)]
    public void Portal_roles_receive_portal_access_only(string roleName)
    {
        var permissions = FeatureCatalog.DefaultFeaturesByRole[roleName];
        Assert.Equal(2, permissions.Count);
        Assert.Contains(FeatureCodes.PortalAccess, permissions);
        Assert.Contains(FeatureCodes.PortalAccountRead, permissions);
    }

    [Fact]
    public void Operational_roles_include_dashboard_view()
    {
        foreach (var roleName in SystemRoles.All.Where(r => !SystemRoles.PortalRoles.Contains(r)))
        {
            var permissions = Permissions(roleName);
            Assert.Contains(FeatureCodes.DashboardView, permissions);
        }
    }

    [Fact]
    public void Receptionist_role_is_limited_to_reception_and_dashboard()
    {
        var permissions = Permissions(SystemRoles.Receptionist);
        Assert.Equal(3, permissions.Count);
        Assert.Contains(FeatureCodes.DashboardView, permissions);
        Assert.Contains(FeatureCodes.ReceptionManage, permissions);
        Assert.Contains(FeatureCodes.ReceptionVisits, permissions);
        Assert.DoesNotContain(FeatureCodes.BillingPayments, permissions);
    }

    [Fact]
    public void Default_features_reference_known_catalog_codes()
    {
        var known = FeatureCodes.All.ToHashSet(StringComparer.Ordinal);

        foreach (var (role, codes) in FeatureCatalog.DefaultFeaturesByRole)
        {
            foreach (var code in codes)
                Assert.True(known.Contains(code), $"Role '{role}' references unknown feature '{code}'.");
        }
    }

    [Fact]
    public void Feature_catalog_matches_feature_codes()
    {
        Assert.Equal(FeatureCodes.All.Count, FeatureCatalog.All.Length);
        foreach (var code in FeatureCodes.All)
            Assert.Contains(FeatureCatalog.All, f => f.Code == code);
    }

    private static HashSet<string> Permissions(string roleName) =>
        FeatureCatalog.DefaultFeaturesByRole[roleName].ToHashSet(StringComparer.Ordinal);
}
