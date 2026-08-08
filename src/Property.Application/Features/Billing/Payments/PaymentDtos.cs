using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Billing.Payments;

public sealed class PaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid FeeChargeId { get; set; }
    public decimal Amount { get; set; }
}

public sealed class PaymentDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<PaymentAllocationDto> Allocations { get; set; } = [];
}

public sealed class CreatePaymentAllocationDto
{
    [Required]
    public Guid FeeChargeId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

public sealed class CreatePaymentDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    public Guid? PersonId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaidAt { get; set; }

    [Required]
    public PaymentMethod Method { get; set; }

    [MaxLength(120)]
    public string? Reference { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreatePaymentAllocationDto> Allocations { get; set; } = [];
}
