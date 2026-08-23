namespace Property.Application.Authorization;

/// <summary>Stable keys for tenant commercial modules (ARCH-002). Not FeatureCodes.</summary>
public static class TenantModuleKeys
{
    public const string Portal = "Portal";
    public const string DigitalPayments = "DigitalPayments";
    public const string Accounting = "Accounting";
    public const string Visits = "Visits";
    public const string Assemblies = "Assemblies";

    public static IReadOnlyList<string> All { get; } =
    [
        Portal,
        DigitalPayments,
        Accounting,
        Visits,
        Assemblies,
    ];
}
