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
        new(Guid.Parse("f1000022-0028-4028-8028-000000000022"), FeatureCodes.StructureNeighborhoods, "Neighborhoods", "Structure", 210),
        new(Guid.Parse("f1000023-0029-4029-8029-000000000023"), FeatureCodes.StructureProperties, "Horizontal properties", "Structure", 220),
        new(Guid.Parse("f1000024-002a-402a-802a-000000000024"), FeatureCodes.StructureBuildings, "Buildings", "Structure", 230),
        new(Guid.Parse("f1000025-002b-402b-802b-000000000025"), FeatureCodes.StructureFloors, "Floors", "Structure", 240),
        new(Guid.Parse("f1000026-002c-402c-802c-000000000026"), FeatureCodes.StructureUnits, "Units", "Structure", 250),
        new(Guid.Parse("f100002f-0035-4035-8035-00000000002f"), FeatureCodes.StructureVehicleBrands, "Vehicle brands", "Structure", 255),
        new(Guid.Parse("f1000027-002d-402d-802d-000000000027"), FeatureCodes.PeopleManage, "People and occupancies", "People", 310),
        new(Guid.Parse("f1000028-002e-402e-802e-000000000028"), FeatureCodes.BillingSettings, "Fee settings", "Billing", 410),
        new(Guid.Parse("f1000029-002f-402f-802f-000000000029"), FeatureCodes.BillingCharges, "Billing periods and charges", "Billing", 420),
        new(Guid.Parse("f100002a-0030-4030-8030-00000000002a"), FeatureCodes.BillingPayments, "Payments", "Billing", 430),
        new(Guid.Parse("f100002b-0031-4031-8031-00000000002b"), FeatureCodes.AmenitiesAreas, "Common areas", "Amenities", 510),
        new(Guid.Parse("f100002c-0032-4032-8032-00000000002c"), FeatureCodes.AmenitiesReservations, "Reservations", "Amenities", 520),
        new(Guid.Parse("f100002d-0033-4033-8033-00000000002d"), FeatureCodes.ReceptionManage, "Reception deliveries and messages", "Reception", 610),
        new(Guid.Parse("f1000030-0036-4036-8036-000000000030"), FeatureCodes.MuralManage, "Community mural notices", "Community", 530),
        new(Guid.Parse("f100002e-0034-4034-8034-00000000002e"), FeatureCodes.ReportsView, "Reports", "Reports", 200),
    ];

    public static IReadOnlyDictionary<string, Guid> IdsByCode { get; } =
        All.ToDictionary(f => f.Code, f => f.Id, StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultFeaturesByRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemRoles.Administrator] = FeatureCodes.All,
            [SystemRoles.Owner] = FeatureCodes.All,
            [SystemRoles.Manager] = FeatureCodes.All,
            [SystemRoles.Receptionist] =
            [
                FeatureCodes.DashboardView,
                FeatureCodes.ReceptionManage,
            ],
        };
}
