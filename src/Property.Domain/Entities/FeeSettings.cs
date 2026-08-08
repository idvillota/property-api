using Property.Domain.Common;

namespace Property.Domain.Entities;

public class FeeSettings : ITenantScoped
{
    public Guid HorizontalPropertyId { get; set; }
    public Guid TenantId { get; set; }
    public decimal MonthlyBudgetAmount { get; set; }
    public int DueDayOfMonth { get; set; } = 5;
    public decimal LateInterestMonthlyPercent { get; set; }
    public string? Notes { get; set; }

    public HorizontalProperty? HorizontalProperty { get; set; }
}
