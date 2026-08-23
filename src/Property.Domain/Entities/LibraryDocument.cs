using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class LibraryDocument : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid FileAssetId { get; set; }
    public LibraryVisibility Visibility { get; set; } = LibraryVisibility.Owners;
    public bool IsActive { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public FileAsset? FileAsset { get; set; }
}
