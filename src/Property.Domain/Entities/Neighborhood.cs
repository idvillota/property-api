using Property.Domain.Common;

namespace Property.Domain.Entities;

public class Neighborhood : NamedTenantEntity
{
    public string? City { get; set; }
    public string Country { get; set; } = "Colombia";
}
