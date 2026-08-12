using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

/// <summary>Vehicle that may be linked to a parking unit (external owners without a dwelling are allowed).</summary>
public class Vehicle : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid VehicleBrandId { get; set; }
    public VehicleType VehicleType { get; set; }
    /// <summary>Denormalized brand name for search/display.</summary>
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Unit? Unit { get; set; }
    public VehicleBrand? VehicleBrand { get; set; }
}
