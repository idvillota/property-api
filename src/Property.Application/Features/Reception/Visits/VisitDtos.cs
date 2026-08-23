using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Reception.Visits;

public sealed class VisitLogDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public Guid VisitorPersonId { get; set; }
    public string VisitorFullName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime EnteredAtUtc { get; set; }
    public VisitLogStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool VisitorWasReused { get; set; }
    public DateTime? ApprovalExpiresAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? OverrideReason { get; set; }
}

public sealed class RegisterVisitDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public DocumentType DocumentType { get; set; }

    [Required]
    [MaxLength(40)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? LastName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>When true (default), visit starts as PendingApproval with a timeout.</summary>
    public bool RequiresApproval { get; set; } = true;
}

public sealed class OverrideApproveVisitDto
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public sealed class VisitIntercomAlertDto
{
    public Guid VisitId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string VisitorFullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string LinkPath { get; set; } = "/portal/visitas";
}
