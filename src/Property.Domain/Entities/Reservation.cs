using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class Reservation : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid CommonAreaId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public int AttendeesCount { get; set; } = 1;
    public string? Notes { get; set; }
    public bool DepositPaid { get; set; }
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public CommonArea? CommonArea { get; set; }
    public Unit? Unit { get; set; }
    public Person? Person { get; set; }
}
