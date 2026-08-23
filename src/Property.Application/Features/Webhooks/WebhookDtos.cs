namespace Property.Application.Features.Webhooks;

public sealed class WebhookSubscriptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public IReadOnlyList<string> EventTypes { get; set; } = [];
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CreateWebhookSubscriptionDto
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public IReadOnlyList<string> EventTypes { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateWebhookSubscriptionDto
{
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public IReadOnlyList<string> EventTypes { get; set; } = [];
    public bool IsActive { get; set; } = true;
}
