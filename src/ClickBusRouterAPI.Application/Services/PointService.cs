using ClickBusRouterAPI.Application.Abstractions;
using ClickBusRouterAPI.Application.Common;
using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.Application.Services;

public sealed class PointService(IMapGraph graph) : IPointService
{
    public IReadOnlyList<PointDto> List(PointType? type = null, string? search = null)
    {
        var term = TextNormalizer.Normalize(search);

        return graph.Points
            .Where(p => type is null || p.Type == type)
            .Where(p => term.Length == 0 || TextNormalizer.Normalize(p.Label).Contains(term, StringComparison.Ordinal))
            .OrderBy(p => p.Type)
            .ThenBy(p => p.Label, StringComparer.OrdinalIgnoreCase)
            .Select(p => p.ToDto())
            .ToList();
    }

    public PointDto? GetById(string id) =>
        graph.TryGetPoint(id, out var point) ? point.ToDto() : null;
}
