using Microsoft.Extensions.DependencyInjection;

namespace Property.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
