namespace Property.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(
        Guid userId,
        Guid tenantId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        Guid? personId = null,
        IReadOnlyList<Guid>? unitIds = null);
}
