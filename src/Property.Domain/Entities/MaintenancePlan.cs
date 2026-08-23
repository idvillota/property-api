using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class MaintenancePlan : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid HorizontalPropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Responsible { get; set; }
    public MaintenanceFrequency Frequency { get; set; } = MaintenanceFrequency.Monthly;
    public bool IsActive { get; set; } = true;
    public HorizontalProperty? HorizontalProperty { get; set; }
    public ICollection<MaintenanceTask> Tasks { get; set; } = new List<MaintenanceTask>();
}
