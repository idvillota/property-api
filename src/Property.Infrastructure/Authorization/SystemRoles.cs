namespace Property.Infrastructure.Authorization;

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Receptionist = "Receptionist";

    /// <summary>Contador PH — módulo contable (ACC-009).</summary>
    public const string Accountant = "Accountant";

    /// <summary>Propietario — portal access (PRT-001).</summary>
    public const string Owner = "Owner";

    /// <summary>Residente — portal access (PRT-001).</summary>
    public const string Resident = "Resident";

    public static IReadOnlyList<string> All { get; } =
        [Administrator, Manager, Receptionist, Accountant, Owner, Resident];

    public static IReadOnlyList<string> PortalRoles { get; } =
        [Owner, Resident];
}
