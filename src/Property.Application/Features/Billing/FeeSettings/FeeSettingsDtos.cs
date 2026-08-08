using System.ComponentModel.DataAnnotations;

namespace Property.Application.Features.Billing.FeeSettings;

public sealed class FeeSettingsDto
{
    public Guid HorizontalPropertyId { get; set; }
    public decimal MonthlyBudgetAmount { get; set; }
    public int DueDayOfMonth { get; set; }
    public decimal LateInterestMonthlyPercent { get; set; }
    public string? Notes { get; set; }
}

public sealed class UpdateFeeSettingsDto
{
    [Range(0, double.MaxValue)]
    public decimal MonthlyBudgetAmount { get; set; }

    [Range(1, 28)]
    public int DueDayOfMonth { get; set; } = 5;

    [Range(0, double.MaxValue)]
    public decimal LateInterestMonthlyPercent { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}
