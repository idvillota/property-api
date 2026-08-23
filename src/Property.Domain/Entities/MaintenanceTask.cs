using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class MaintenanceTask : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid MaintenancePlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public MaintenanceTaskStatus Status { get; set; } = MaintenanceTaskStatus.Pending;
    public MaintenancePlan? MaintenancePlan { get; set; }
}
