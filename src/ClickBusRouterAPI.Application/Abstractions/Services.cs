using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.Application.Abstractions;

public interface IRouteService
{
    RouteResult CalculateFastestRoute(RouteQuery query);
}

public interface IPointService
{
    /// <param name="type">Filtra pela categoria; nulo traz todas.</param>
    /// <param name="search">Trecho do nome, sem diferenciar acento ou maiúsculas; nulo traz todos.</param>
    IReadOnlyList<PointDto> List(PointType? type = null, string? search = null);

    PointDto? GetById(string id);
}
