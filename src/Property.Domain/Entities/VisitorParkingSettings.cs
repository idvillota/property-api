using Property.Domain.Common;

namespace Property.Domain.Entities;

public class VisitorParkingSettings : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public int TotalSpots { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? DailyCap { get; set; }
    public bool IsActive { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
}
