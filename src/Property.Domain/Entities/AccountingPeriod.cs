using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class AccountingPeriod : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;
    public DateTime? ClosedAtUtc { get; set; }
    public HorizontalProperty? HorizontalProperty { get; set; }
}
