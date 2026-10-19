using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Infrastructure.Map;

namespace ClickBusRouterAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MapDataOptions>(configuration.GetSection(MapDataOptions.SectionName));
        services.AddSingleton<JsonMapGraphLoader>();

        // Construído uma vez e reutilizado por toda a vida da aplicação.
        services.AddSingleton<IMapGraph>(sp => sp.GetRequiredService<JsonMapGraphLoader>().Load());

        return services;
    }
}
