using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;

namespace Property.Infrastructure.Identity;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public const string PermissionClaimType = PermissionClaims.Type;

    public string CreateAccessToken(
        Guid userId,
        Guid tenantId,
        string email,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions,
        Guid? personId = null,
        IReadOnlyList<Guid>? unitIds = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("tenant_id", tenantId.ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(PermissionClaimType, p)));

        if (personId is Guid pid && pid != Guid.Empty)
        {
            claims.Add(new Claim(PortalClaims.PersonId, pid.ToString()));
            if (unitIds is { Count: > 0 })
            {
                foreach (var unitId in unitIds.Distinct())
                    claims.Add(new Claim(PortalClaims.UnitId, unitId.ToString()));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
