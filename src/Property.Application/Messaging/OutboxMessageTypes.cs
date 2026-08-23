namespace Property.Application.Messaging;

/// <summary>Stable outbox message type keys (ARCH-003).</summary>
public static class OutboxMessageTypes
{
    public const string EmailSend = "email.send";
    public const string ChargePosted = "charge.posted";
    public const string PaymentPosted = "payment.posted";
    public const string WhatsAppSend = "whatsapp.send";
    public const string WebhookDeliver = "webhook.deliver";
}
