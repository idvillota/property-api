using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class ParkingTicket : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid ParkingShiftId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public DateTime EnteredAtUtc { get; set; }
    public DateTime? ExitedAtUtc { get; set; }
    public decimal? FeeAmount { get; set; }
    public ParkingTicketStatus Status { get; set; } = ParkingTicketStatus.Open;
    public ParkingShift? ParkingShift { get; set; }
    public HorizontalProperty? HorizontalProperty { get; set; }
}
