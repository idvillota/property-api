using Property.Application.Features.Organization.TenantUsers;

namespace Property.Application.Common.Interfaces;

public interface ITenantUserInviteService
{
    Task<InvitedTenantUserDto> InviteAsync(InviteTenantUserDto dto, CancellationToken cancellationToken = default);
}
