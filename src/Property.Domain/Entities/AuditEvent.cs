using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>Append-only audit trail (ARCH-006). Never update or delete.</summary>
public class AuditEvent : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public User? ActorUser { get; set; }
}
