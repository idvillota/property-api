using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class ReceptionItem : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public ReceptionItemType ItemType { get; set; }
    public ReceptionItemStatus Status { get; set; } = ReceptionItemStatus.PendingPickup;
    public DateTime ReceivedAtUtc { get; set; }
    public Guid? ReceivedByEmployeeId { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public Guid? DeliveredByEmployeeId { get; set; }
    public string? CarrierOrSender { get; set; }
    public string? TrackingOrReference { get; set; }
    public string? RecipientName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Unit? Unit { get; set; }
    public Person? Person { get; set; }
    public Employee? ReceivedByEmployee { get; set; }
    public Employee? DeliveredByEmployee { get; set; }
}
