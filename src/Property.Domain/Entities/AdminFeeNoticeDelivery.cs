using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>One administration-fee email already queued for a charge and address.</summary>
public class AdminFeeNoticeDelivery : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid FeeChargeId { get; set; }
    public string ToEmail { get; set; } = string.Empty;

    public FeeCharge? FeeCharge { get; set; }
}
