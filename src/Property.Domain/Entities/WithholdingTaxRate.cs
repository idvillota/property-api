using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class WithholdingTaxRate : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public WithholdingKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public decimal MinimumBase { get; set; }
    public bool IsActive { get; set; } = true;
}
