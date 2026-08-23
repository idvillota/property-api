using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Billing.FeeCharges;

public sealed class FeeChargeDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid BillingPeriodId { get; set; }
    public Guid UnitId { get; set; }
    public FeeChargeType ChargeType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal CoefficientSnapshot { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly DueDate { get; set; }
    public FeeChargeStatus Status { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateFeeChargeDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid BillingPeriodId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public FeeChargeType ChargeType { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }
}

public sealed class UpdateFeeChargeDto
{
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateOnly DueDate { get; set; }

    public FeeChargeStatus? Status { get; set; }

    /// <summary>Allowed only for Adjustment charges.</summary>
    [Range(0, double.MaxValue)]
    public decimal? Amount { get; set; }

    public bool? IsActive { get; set; }
}

public sealed class GenerateOrdinaryChargesDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid BillingPeriodId { get; set; }
}

public sealed class GenerateInterestChargesDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? BillingPeriodId { get; set; }
}

public sealed class GenerateChargesResultDto
{
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>BIL-014 — dry-run totals for ordinary cuota liquidación.</summary>
public sealed class LiquidationPreviewDto
{
    public Guid BillingPeriodId { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public bool IsLiquidated { get; set; }
    public int BillableUnitCount { get; set; }
    public int ChargeCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MonthlyBudgetAmount { get; set; }
    public DateOnly DueDate { get; set; }
}

public sealed class LiquidationCommitDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid BillingPeriodId { get; set; }
}

public sealed class LiquidationResultDto
{
    public Guid BillingPeriodId { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsLiquidated { get; set; }
    public DateTime? LiquidatedAtUtc { get; set; }
}

public sealed class ArrearsSummaryDto
{
    public decimal TotalPending { get; set; }
    public decimal TotalPartial { get; set; }
    public decimal TotalOverdue { get; set; }
    public int UnitCountInArrears { get; set; }
}
