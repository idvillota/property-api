using Property.Domain.Common;

namespace Property.Domain.Entities;

public class Vendor : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}
