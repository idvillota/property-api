namespace Property.Application.Messaging;

public sealed class WebhookDeliverPayload
{
    public Guid SubscriptionId { get; set; }
    public string TargetUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string BodyJson { get; set; } = string.Empty;
}
