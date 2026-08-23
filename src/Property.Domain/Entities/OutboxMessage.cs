using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>Transactional outbox row (ARCH-003). Written in the same DB transaction as the use case.</summary>
public class OutboxMessage : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }

    /// <summary>Stable type key, e.g. <c>email.send</c>.</summary>
    public string MessageType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; } = 5;

    /// <summary>When null or in the past, the message is eligible for dispatch.</summary>
    public DateTime? NextAttemptUtc { get; set; }

    public string? LastError { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
