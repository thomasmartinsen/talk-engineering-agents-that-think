using Microsoft.Extensions.DependencyInjection;

namespace Agents;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddAgentSetup(this IServiceCollection services)
    {
        return services;
    }
}
