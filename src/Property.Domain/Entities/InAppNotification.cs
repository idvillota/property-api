using Property.Domain.Common;

namespace Property.Domain.Entities;

/// <summary>In-app notification for portal/intercom (REC-012).</summary>
public class InAppNotification : EntityBase, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PersonId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? LinkPath { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }

    public User? User { get; set; }
    public Person? Person { get; set; }
}
