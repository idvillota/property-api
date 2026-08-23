using Property.Domain.Common;
using Property.Domain.Enums;

namespace Property.Domain.Entities;

public class Person : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    /// <summary>DIFF-003 — opt-in for operational WhatsApp.</summary>
    public bool WhatsAppOptIn { get; set; }
    public bool IsActive { get; set; } = true;
}
