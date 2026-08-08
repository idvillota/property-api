using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class FeeCharge : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid BillingPeriodId { get; set; }
    public Guid UnitId { get; set; }
    public FeeChargeType ChargeType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CoefficientSnapshot { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly DueDate { get; set; }
    public FeeChargeStatus Status { get; set; } = FeeChargeStatus.Pending;
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public BillingPeriod? BillingPeriod { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
