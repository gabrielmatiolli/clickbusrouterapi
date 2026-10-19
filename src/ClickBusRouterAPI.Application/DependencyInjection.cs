using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClickBusRouterAPI.Application.Abstractions;
using ClickBusRouterAPI.Application.Options;
using ClickBusRouterAPI.Application.Services;

namespace ClickBusRouterAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RoutingOptions>()
            .Bind(configuration.GetSection(RoutingOptions.SectionName))
            .Validate(o => o.MaxSnapDistance > 0, "Routing:MaxSnapDistance deve ser maior que zero.")
            .Validate(o => o.MetersPerMapUnit > 0, "Routing:MetersPerMapUnit deve ser maior que zero.")
            .Validate(o => o.WalkingSpeedMetersPerSecond > 0, "Routing:WalkingSpeedMetersPerSecond deve ser maior que zero.")
            .ValidateOnStart();

        // Serviços sem estado sobre um grafo imutável: singleton é seguro e evita alocações por request.
        services.AddSingleton<IRouteService, RouteService>();
        services.AddSingleton<IPointService, PointService>();

        return services;
    }
}
