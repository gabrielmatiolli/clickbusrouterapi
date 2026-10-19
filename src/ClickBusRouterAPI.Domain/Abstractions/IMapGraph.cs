using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.Domain.Abstractions;

/// <summary>
/// Grafo imutável do mapa, carregado uma única vez em memória.
/// </summary>
public interface IMapGraph
{
    IReadOnlyCollection<MapPoint> Points { get; }

    ImmutableArray<Segment> Segments { get; }

    bool TryGetPoint(string id, [MaybeNullWhen(false)] out MapPoint point);

    ImmutableArray<Edge> GetNeighbors(string pointId);
}
