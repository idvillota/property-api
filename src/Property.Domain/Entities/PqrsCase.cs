using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>COM-012/013 — PQR/S case (petición, queja, reclamo, sugerencia).</summary>
public class PqrsCase : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid CreatedByPersonId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public PqrsCategory Category { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PqrsStatus Status { get; set; } = PqrsStatus.Open;
    public DateTime? DueAtUtc { get; set; }
    public Guid? AssignedToUserId { get; set; }
    /// <summary>JSON Guid[] of FileAsset ids (optional attachments).</summary>
    public string AttachmentFileAssetIdsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
    public Person? CreatedByPerson { get; set; }
    public ICollection<PqrsComment> Comments { get; set; } = new List<PqrsComment>();
}
