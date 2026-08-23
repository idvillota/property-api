namespace Property.Application.Auditing;

public static class AuditActions
{
    public const string RolePermissionsUpdated = "RolePermissions.Updated";
    public const string PaymentCreated = "Payment.Created";
    public const string PspWebhookInvalidSignature = "PspWebhook.InvalidSignature";
    public const string PspWebhookProcessed = "PspWebhook.Processed";
}

public static class AuditEntityTypes
{
    public const string Role = "Role";
    public const string Payment = "Payment";
    public const string PaymentIntent = "PaymentIntent";
    public const string PspWebhook = "PspWebhook";
}
