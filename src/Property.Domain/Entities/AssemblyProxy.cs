using Property.Domain.Common;

namespace Property.Domain.Entities;

public class AssemblyProxy : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AssemblyId { get; set; }
    public Guid GrantorUnitId { get; set; }
    public Guid ProxyPersonId { get; set; }
    public decimal CoefficientSnapshot { get; set; }
    public bool IsActive { get; set; } = true;
    public Assembly? Assembly { get; set; }
    public Unit? GrantorUnit { get; set; }
    public Person? ProxyPerson { get; set; }
}
