using Property.Application.Features.Auth;

namespace Property.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterTenantAsync(RegisterTenantDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}
