using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Property.Application.Common.Interfaces;

namespace Property.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentTenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantClaim, out var tenantId))
                tenantContext.TenantId = tenantId;

            var userSub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            if (Guid.TryParse(userSub, out var userId))
                tenantContext.UserId = userId;
        }

        await next(context);
    }
}
