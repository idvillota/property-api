using Property.Application.Authorization;

namespace Property.Infrastructure.Authorization;

public sealed record FeatureDefinition(Guid Id, string Code, string Name, string Module, int SortOrder);

public static class FeatureCatalog
{
    public static readonly FeatureDefinition[] All =
    [
        new(Guid.Parse("f1000001-0001-4001-8001-000000000001"), FeatureCodes.DashboardView, "Dashboard", "Dashboard", 10),
        new(Guid.Parse("f1000021-0027-4027-8027-000000000021"), FeatureCodes.DashboardConfigure, "Configure dashboard", "Administration", 11),
        new(Guid.Parse("f100000b-0011-4011-8011-00000000000b"), FeatureCodes.SettingsTenant, "Tenant settings", "Administration", 110),
        new(Guid.Parse("f100000e-0014-4014-8014-00000000000e"), FeatureCodes.OrganizationEmployees, "Employees", "Administration", 140),
        new(Guid.Parse("f100000f-0015-4015-8015-00000000000f"), FeatureCodes.OrganizationTeam, "Invite team", "Administration", 150),
        new(Guid.Parse("f1000010-0016-4016-8016-000000000010"), FeatureCodes.OrganizationRoles, "Roles and permissions", "Administration", 160),
    ];

    public static IReadOnlyDictionary<string, Guid> IdsByCode { get; } =
        All.ToDictionary(f => f.Code, f => f.Id, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultFeaturesByRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemRoles.Administrator] = FeatureCodes.All,
            [SystemRoles.Owner] = FeatureCodes.All,
            [SystemRoles.Manager] = FeatureCodes.All,
        };
}
