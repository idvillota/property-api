using Property.Domain.Common;

namespace Property.Domain.Entities;

public class AgendaItem : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AssemblyId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Assembly? Assembly { get; set; }
}
