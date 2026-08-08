using Property.Domain.Common;

namespace Property.Domain.Entities;

public class PaymentAllocation : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid FeeChargeId { get; set; }
    public decimal Amount { get; set; }

    public Payment? Payment { get; set; }
    public FeeCharge? FeeCharge { get; set; }
}
