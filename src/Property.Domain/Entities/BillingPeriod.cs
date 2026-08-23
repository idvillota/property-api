using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class BillingPeriod : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public BillingPeriodStatus Status { get; set; } = BillingPeriodStatus.Open;
    public string? Notes { get; set; }
    /// <summary>True after ordinary cuota liquidación (BIL-014); blocks re-liquidation.</summary>
    public bool IsLiquidated { get; set; }
    public DateTime? LiquidatedAtUtc { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<FeeCharge> FeeCharges { get; set; } = new List<FeeCharge>();
}
