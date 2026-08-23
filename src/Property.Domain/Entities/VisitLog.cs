using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>Visitor entry in the portería libro (REC-010 / REC-011).</summary>
public class VisitLog : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid VisitorPersonId { get; set; }
    public DateTime EnteredAtUtc { get; set; }
    public VisitLogStatus Status { get; set; } = VisitLogStatus.Registered;
    public Guid? RegisteredByUserId { get; set; }
    public string? Notes { get; set; }

    /// <summary>When PendingApproval expires if no decision (REC-011).</summary>
    public DateTime? ApprovalExpiresAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedByPersonId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? OverrideReason { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
    public Person? VisitorPerson { get; set; }
    public User? RegisteredByUser { get; set; }
    public Person? DecidedByPerson { get; set; }
    public User? DecidedByUser { get; set; }
}
