using Property.Domain.Common;

namespace Property.Domain.Entities;

public class Building : NamedTenantEntity
{
    public Guid HorizontalPropertyId { get; set; }
    public string? Code { get; set; }
    public int SortOrder { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
