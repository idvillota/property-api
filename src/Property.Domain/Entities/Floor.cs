using Property.Domain.Common;

namespace Property.Domain.Entities;

public class Floor : NamedTenantEntity
{
    public Guid BuildingId { get; set; }
    public int? Number { get; set; }
    public int SortOrder { get; set; }

    public Building? Building { get; set; }
}
