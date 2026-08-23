using Property.Domain.Common;

namespace Property.Domain.Entities;

public class AssemblyAttendance : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AssemblyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? PersonId { get; set; }
    public decimal CoefficientSnapshot { get; set; }
    public bool Present { get; set; } = true;
    public Assembly? Assembly { get; set; }
    public Unit? Unit { get; set; }
    public Person? Person { get; set; }
}
