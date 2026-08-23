namespace Property.Application.Auditing;

public static class AuditActions
{
    public const string RolePermissionsUpdated = "RolePermissions.Updated";
    public const string PaymentCreated = "Payment.Created";
}

public static class AuditEntityTypes
{
    public const string Role = "Role";
    public const string Payment = "Payment";
}
