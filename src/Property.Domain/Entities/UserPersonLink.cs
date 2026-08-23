using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>
/// Links a global User to a tenant Person for portal scoping (unit claims).
/// </summary>
public class UserPersonLink : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid PersonId { get; set; }
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public Person? Person { get; set; }
}
