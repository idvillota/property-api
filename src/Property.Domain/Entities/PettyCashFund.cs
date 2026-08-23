using Property.Domain.Common;

namespace Property.Domain.Entities;

public class PettyCashFund : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
}
