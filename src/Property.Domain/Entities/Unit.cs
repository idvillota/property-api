using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class Unit : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? FloorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public UnitType UnitType { get; set; }
    public decimal PrivateBuiltAreaM2 { get; set; }
    public decimal? PrivateOpenAreaM2 { get; set; }
    public decimal Coefficient { get; set; }
    public string? ContributionModuleCode { get; set; }
    public Guid? LinkedUnitId { get; set; }
    public bool IsBillable { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public HorizontalProperty? HorizontalProperty { get; set; }
    public Building? Building { get; set; }
    public Floor? Floor { get; set; }
    public Unit? LinkedUnit { get; set; }
}
