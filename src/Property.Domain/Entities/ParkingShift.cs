using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class ParkingShift : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? OpenedByUserId { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public ParkingShiftStatus Status { get; set; } = ParkingShiftStatus.Open;
    public decimal CollectedTotal { get; set; }
    public decimal? DeclaredCash { get; set; }
    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<ParkingTicket> Tickets { get; set; } = new List<ParkingTicket>();
}
