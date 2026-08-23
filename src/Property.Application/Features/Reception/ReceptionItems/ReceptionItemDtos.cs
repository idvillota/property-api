using System.ComponentModel.DataAnnotations;
using Property.Domain.Enums;

namespace Property.Application.Features.Reception.ReceptionItems;

public sealed class ReceptionItemDto
{
    public Guid Id { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public ReceptionDirection Direction { get; set; }
    public ReceptionItemType ItemType { get; set; }
    public ReceptionItemStatus Status { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public Guid? ReceivedByEmployeeId { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public Guid? DeliveredByEmployeeId { get; set; }
    public Guid? DeliveredToPersonId { get; set; }
    public string? CarrierOrSender { get; set; }
    public string? TrackingOrReference { get; set; }
    public string? RecipientName { get; set; }
    public string? AuthorizedPickupName1 { get; set; }
    public string? AuthorizedPickupName2 { get; set; }
    public string? DeliveredToExternalName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public string? UnitCode { get; set; }
    public string? PersonName { get; set; }
    public string? DeliveredToPersonName { get; set; }
}

public sealed class CreateReceptionItemDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public ReceptionDirection Direction { get; set; } = ReceptionDirection.Inbound;

    [Required]
    public ReceptionItemType ItemType { get; set; }

    public ReceptionItemStatus Status { get; set; } = ReceptionItemStatus.PendingPickup;

    public Guid? ReceivedByEmployeeId { get; set; }

    [MaxLength(200)]
    public string? CarrierOrSender { get; set; }

    [MaxLength(120)]
    public string? TrackingOrReference { get; set; }

    [MaxLength(200)]
    public string? AuthorizedPickupName1 { get; set; }

    [MaxLength(200)]
    public string? AuthorizedPickupName2 { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    /// <summary>REC-020 — enqueue email to unit contact on create (default true).</summary>
    public bool Notify { get; set; } = true;
}

public sealed class UpdateReceptionItemDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Required]
    public Guid PersonId { get; set; }

    [Required]
    public ReceptionDirection Direction { get; set; }

    [Required]
    public ReceptionItemType ItemType { get; set; }

    [Required]
    public ReceptionItemStatus Status { get; set; }

    public Guid? ReceivedByEmployeeId { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }

    public Guid? DeliveredByEmployeeId { get; set; }

    public Guid? DeliveredToPersonId { get; set; }

    [MaxLength(200)]
    public string? CarrierOrSender { get; set; }

    [MaxLength(120)]
    public string? TrackingOrReference { get; set; }

    [MaxLength(200)]
    public string? AuthorizedPickupName1 { get; set; }

    [MaxLength(200)]
    public string? AuthorizedPickupName2 { get; set; }

    [MaxLength(200)]
    public string? DeliveredToExternalName { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class DeliverReceptionItemDto
{
    /// <summary>Inbound: habitant who receives. Required for inbound.</summary>
    public Guid? DeliveredToPersonId { get; set; }

    /// <summary>Outbound: must match AuthorizedPickupName1 or AuthorizedPickupName2.</summary>
    [MaxLength(200)]
    public string? DeliveredToExternalName { get; set; }

    public Guid? DeliveredByEmployeeId { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }
}

public sealed class ReceptionPendingCountDto
{
    public int PendingPickupCount { get; set; }
}

public sealed class BulkUtilityBillsDto
{
    [Required]
    public Guid HorizontalPropertyId { get; set; }

    [Required]
    public ReceptionItemType BillType { get; set; }

    /// <summary>Billing period as yyyy-MM (defaults to current UTC month).</summary>
    [MaxLength(7)]
    public string? Period { get; set; }
}

public sealed class BulkUtilityBillsResultDto
{
    public string Period { get; set; } = "";
    public ReceptionItemType BillType { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedAlreadyExistsCount { get; set; }
    public int SkippedNoHabitantCount { get; set; }
    public int DwellingUnitCount { get; set; }
}
