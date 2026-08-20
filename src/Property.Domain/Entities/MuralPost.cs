using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>Community bulletin post (Mural): official notices and neighbor information.</summary>
public class MuralPost : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public MuralPostKind Kind { get; set; } = MuralPostKind.Community;
    public MuralPostCategory Category { get; set; } = MuralPostCategory.Other;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactNote { get; set; }
    public string? AreaLabel { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string AuthorDisplayName { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsHidden { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
}
