using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Amenities.Reservations;

public sealed class ReservationDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid CommonAreaId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public ReservationStatus Status { get; set; }
    public int AttendeesCount { get; set; }
    public string? Notes { get; set; }
    public bool DepositPaid { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateReservationDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid CommonAreaId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    public Guid? PersonId { get; set; }

    [Required]
    public DateTime StartsAtUtc { get; set; }

    [Required]
    public DateTime EndsAtUtc { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [Range(1, int.MaxValue)]
    public int AttendeesCount { get; set; } = 1;

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public bool DepositPaid { get; set; }
}

public sealed class UpdateReservationDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid CommonAreaId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    public Guid? PersonId { get; set; }

    [Required]
    public DateTime StartsAtUtc { get; set; }

    [Required]
    public DateTime EndsAtUtc { get; set; }

    [Required]
    public ReservationStatus Status { get; set; }

    [Range(1, int.MaxValue)]
    public int AttendeesCount { get; set; } = 1;

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public bool DepositPaid { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class ChangeReservationStatusDto
{
    [Required]
    public ReservationStatus Status { get; set; }
}
