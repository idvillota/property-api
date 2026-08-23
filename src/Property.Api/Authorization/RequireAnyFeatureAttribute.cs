using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Property.Application.Authorization;
using Property.Infrastructure.Identity;

namespace Property.Api.Authorization;

/// <summary>Allows access when the user has <em>any</em> of the listed feature codes.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireAnyFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    public RequireAnyFeatureAttribute(params string[] featureCodes)
    {
        if (featureCodes is null || featureCodes.Length == 0)
            throw new ArgumentException("At least one feature code is required.", nameof(featureCodes));
        FeatureCodes = featureCodes;
    }

    public IReadOnlyList<string> FeatureCodes { get; }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        var held = user.FindAll(PermissionClaims.Type).Select(c => c.Value).ToHashSet(StringComparer.Ordinal);
        if (!FeatureCodes.Any(code => held.Contains(code)))
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
