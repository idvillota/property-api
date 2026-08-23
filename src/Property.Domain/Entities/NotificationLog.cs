using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>DIFF-003 — channel used for operational notifications.</summary>
public class NotificationLog : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid? HorizontalPropertyId { get; set; }
    public Guid? PersonId { get; set; }
    public Guid? UnitId { get; set; }
    public string Channel { get; set; } = string.Empty; // email | whatsapp
    public string Purpose { get; set; } = string.Empty; // dunning | visit
    public string? ToAddress { get; set; }
    public string? TemplateKey { get; set; }
    public Guid? OutboxMessageId { get; set; }
    public DateTime SentAtUtc { get; set; }
}
