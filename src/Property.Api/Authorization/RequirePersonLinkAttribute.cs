using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Property.Application.Common.Interfaces;

namespace Property.Api.Authorization;

/// <summary>
/// Blocks portal endpoints when the current user has no active UserPersonLink (ARCH-001).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePersonLinkAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ICurrentTenantContext>();
        var links = context.HttpContext.RequestServices.GetRequiredService<IUserPersonLinkService>();

        if (tenant.UserId is not Guid userId)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var (personId, _) = await links.GetPortalScopeAsync(userId, context.HttpContext.RequestAborted);
        if (personId is null)
        {
            context.Result = new ObjectResult(new { message = "Person link required for portal access." })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            return;
        }

        await next();
    }
}
