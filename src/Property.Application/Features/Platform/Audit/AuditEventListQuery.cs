namespace Property.Application.Features.Platform.Audit;

public sealed class AuditEventListQuery
{
    public Guid? ActorUserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int Take { get; set; } = 50;
}
