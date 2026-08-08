using Microsoft.AspNetCore.Authorization;
using Property.Application.Authorization;

namespace Property.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireFeatureAttribute : AuthorizeAttribute
{
    public RequireFeatureAttribute(string featureCode)
    {
        Policy = FeatureAuthorizationPolicies.For(featureCode);
    }
}
