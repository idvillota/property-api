using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class UnitOccupancy : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PersonId { get; set; }
    public OccupancyRole Role { get; set; }
    public bool IsPrimary { get; set; }
    public bool ReceivesBilling { get; set; }
    /// <summary>Receives unit-level communications such as utility bill arrivals.</summary>
    public bool IsCommunicationResponsible { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Unit? Unit { get; set; }
    public Person? Person { get; set; }
}