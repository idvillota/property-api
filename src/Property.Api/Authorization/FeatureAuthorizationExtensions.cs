using Microsoft.AspNetCore.Authorization;
using Property.Application.Authorization;

namespace Property.Api.Authorization;

public static class FeatureAuthorizationExtensions
{
    public static IServiceCollection AddFeatureAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var code in FeatureCodes.All)
            {
                options.AddPolicy(
                    FeatureAuthorizationPolicies.For(code),
                    policy => policy
                        .RequireAuthenticatedUser()
                        .RequireClaim(PermissionClaims.Type, code));
            }

            options.AddPolicy(
                PlatformAuthorizationPolicies.PlatformAdmin,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PlatformAdminRequirement()));
        });

        services.AddSingleton<IAuthorizationHandler, PlatformAdminAuthorizationHandler>();

        return services;
    }
}
