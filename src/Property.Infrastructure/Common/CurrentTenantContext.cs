using Property.Application.Common.Interfaces;

namespace Property.Infrastructure.Common;

public sealed class CurrentTenantContext : ICurrentTenantContext
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
}
