using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>COM-013 — comment / reply on a PQRS case (timeline).</summary>
public class PqrsComment : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid PqrsCaseId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public Guid? AuthorPersonId { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime PostedAtUtc { get; set; }

    public PqrsCase? PqrsCase { get; set; }
}
