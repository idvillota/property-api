using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>DIFF-006 — partner webhook endpoint.</summary>
public class WebhookSubscription : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    /// <summary>Comma-separated event types, e.g. PaymentPosted,VisitApproved,PqrsCreated.</summary>
    public string EventTypes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
