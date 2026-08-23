using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>COM-011 — one read ack per person per mural post.</summary>
public class MuralPostRead : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid MuralPostId { get; set; }
    public Guid PersonId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime ReadAtUtc { get; set; }

    public MuralPost? MuralPost { get; set; }
    public Person? Person { get; set; }
}
