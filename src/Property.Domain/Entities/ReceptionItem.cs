using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class ReceptionItem : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public ReceptionDirection Direction { get; set; } = ReceptionDirection.Inbound;
    public ReceptionItemType ItemType { get; set; }
    public ReceptionItemStatus Status { get; set; } = ReceptionItemStatus.PendingPickup;
    public DateTime ReceivedAtUtc { get; set; }
    public Guid? ReceivedByEmployeeId { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public Guid? DeliveredByEmployeeId { get; set; }
    public Guid? DeliveredToPersonId { get; set; }
    public string? CarrierOrSender { get; set; }
    public string? TrackingOrReference { get; set; }
    public string? RecipientName { get; set; }
    /// <summary>Outbound: first external person authorized to pick up (required for outbound).</summary>
    public string? AuthorizedPickupName1 { get; set; }
    /// <summary>Outbound: optional second external person authorized to pick up.</summary>
    public string? AuthorizedPickupName2 { get; set; }
    /// <summary>Outbound: which authorized external person actually received the item.</summary>
    public string? DeliveredToExternalName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
    public Person? Person { get; set; }
    public Person? DeliveredToPerson { get; set; }
    public Employee? ReceivedByEmployee { get; set; }
    public Employee? DeliveredByEmployee { get; set; }
}
