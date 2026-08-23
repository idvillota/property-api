using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Property.Application.Common.Interfaces;

namespace Property.Api.Authorization;

/// <summary>Blocks endpoints when the tenant module flag is off (ARCH-002).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireTenantModuleAttribute : Attribute, IAsyncActionFilter
{
    public RequireTenantModuleAttribute(string moduleKey)
    {
        ModuleKey = moduleKey;
    }

    public string ModuleKey { get; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var modules = context.HttpContext.RequestServices.GetRequiredService<ITenantModuleService>();
        var enabled = await modules.IsEnabledAsync(ModuleKey, context.HttpContext.RequestAborted);
        if (!enabled)
        {
            context.Result = new ObjectResult(new
            {
                message = $"Tenant module '{ModuleKey}' is disabled.",
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            return;
        }

        await next();
    }
}
