using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Reception.ReceptionItems;

public sealed class ReceptionItemDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public ReceptionItemType ItemType { get; set; }
    public ReceptionItemStatus Status { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public Guid? ReceivedByEmployeeId { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public Guid? DeliveredByEmployeeId { get; set; }
    public string? CarrierOrSender { get; set; }
    public string? TrackingOrReference { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateReceptionItemDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? UnitId { get; set; }

    public Guid? PersonId { get; set; }

    [Required]
    public ReceptionItemType ItemType { get; set; }

    public ReceptionItemStatus Status { get; set; } = ReceptionItemStatus.PendingPickup;

    [Required]
    public DateTime ReceivedAtUtc { get; set; }

    public Guid? ReceivedByEmployeeId { get; set; }

    [MaxLength(200)]
    public string? CarrierOrSender { get; set; }

    [MaxLength(120)]
    public string? TrackingOrReference { get; set; }

    [MaxLength(200)]
    public string? RecipientName { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class UpdateReceptionItemDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    public Guid? UnitId { get; set; }

    public Guid? PersonId { get; set; }

    [Required]
    public ReceptionItemType ItemType { get; set; }

    [Required]
    public ReceptionItemStatus Status { get; set; }

    [Required]
    public DateTime ReceivedAtUtc { get; set; }

    public Guid? ReceivedByEmployeeId { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }

    public Guid? DeliveredByEmployeeId { get; set; }

    [MaxLength(200)]
    public string? CarrierOrSender { get; set; }

    [MaxLength(120)]
    public string? TrackingOrReference { get; set; }

    [MaxLength(200)]
    public string? RecipientName { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class DeliverReceptionItemDto
{
    public Guid? DeliveredByEmployeeId { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class ReceptionPendingCountDto
{
    public int PendingPickupCount { get; set; }
}
