namespace Property.Application.Authorization;

public static class FeatureCodes
{
    public const string DashboardView = "dashboard.view";
    public const string DashboardConfigure = "dashboard.configure";
    public const string SettingsTenant = "settings.tenant";
    public const string OrganizationEmployees = "organization.employees";
    public const string OrganizationTeam = "organization.team";
    public const string OrganizationRoles = "organization.roles";
    public const string StructureNeighborhoods = "structure.neighborhoods";
    public const string StructureProperties = "structure.properties";
    public const string StructureBuildings = "structure.buildings";
    public const string StructureFloors = "structure.floors";
    public const string StructureUnits = "structure.units";
    public const string StructureVehicleBrands = "structure.vehicleBrands";
    public const string PeopleManage = "people.manage";
    public const string BillingSettings = "billing.settings";
    public const string BillingCharges = "billing.charges";
    public const string BillingPayments = "billing.payments";
    public const string AmenitiesAreas = "amenities.areas";
    public const string AmenitiesReservations = "amenities.reservations";
    public const string ReceptionManage = "reception.manage";
    public const string ReceptionVisits = "reception.visits";
    public const string ReceptionIntercom = "reception.intercom";
    public const string MuralManage = "mural.manage";
    public const string PqrsCreate = "pqrs.create";
    public const string PqrsManage = "pqrs.manage";
    public const string ReportsView = "reports.view";
    public const string AuditView = "audit.view";
    public const string PortalAccess = "portal.access";
    public const string PortalAccountRead = "portal.account.read";
    public const string PortalReservations = "portal.reservations";
    public const string PortalPay = "portal.pay";
    public const string PortalMuralRead = "portal.mural.read";
    public const string PortalVisits = "portal.visits";
    public const string AccountingCoa = "accounting.coa";
    public const string AccountingJournals = "accounting.journals";
    public const string AccountingBudget = "accounting.budget";
    public const string AccountingAp = "accounting.ap";
    public const string AccountingPettyCash = "accounting.pettycash";
    public const string AccountingTax = "accounting.tax";
    public const string AccountingReports = "accounting.reports";
    public const string AccountingBank = "accounting.bank";

    public static IReadOnlyList<string> All { get; } =
    [
        DashboardView,
        DashboardConfigure,
        SettingsTenant,
        OrganizationEmployees,
        OrganizationTeam,
        OrganizationRoles,
        StructureNeighborhoods,
        StructureProperties,
        StructureBuildings,
        StructureFloors,
        StructureUnits,
        StructureVehicleBrands,
        PeopleManage,
        BillingSettings,
        BillingCharges,
        BillingPayments,
        AmenitiesAreas,
        AmenitiesReservations,
        ReceptionManage,
        ReceptionVisits,
        ReceptionIntercom,
        MuralManage,
        PqrsCreate,
        PqrsManage,
        ReportsView,
        AuditView,
        PortalAccess,
        PortalAccountRead,
        PortalReservations,
        PortalPay,
        PortalMuralRead,
        PortalVisits,
        AccountingCoa,
        AccountingJournals,
        AccountingBudget,
        AccountingAp,
        AccountingPettyCash,
        AccountingTax,
        AccountingReports,
        AccountingBank,
    ];

    public static IReadOnlyList<string> AccountingAll { get; } =
    [
        AccountingCoa,
        AccountingJournals,
        AccountingBudget,
        AccountingAp,
        AccountingPettyCash,
        AccountingTax,
        AccountingReports,
        AccountingBank,
    ];
}
