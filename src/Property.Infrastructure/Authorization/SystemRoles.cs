namespace Property.Infrastructure.Authorization;

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";

    /// <summary>Legacy name kept for migrating existing databases.</summary>
    public const string Owner = "Owner";

    public static IReadOnlyList<string> All { get; } =
        [Administrator, Manager];
}
