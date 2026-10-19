using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.Infrastructure.Map;

/// <summary>
/// Grafo somente leitura. <see cref="FrozenDictionary{TKey,TValue}"/> é otimizado para
/// "escreve uma vez, lê muitas": a criação é mais cara, mas cada lookup fica mais rápido que
/// num Dictionary comum, e ele é thread-safe para leitura sem lock.
/// </summary>
internal sealed class InMemoryMapGraph(
    FrozenDictionary<string, MapPoint> points,
    FrozenDictionary<string, ImmutableArray<Edge>> adjacency,
    ImmutableArray<Segment> segments) : IMapGraph
{
    public IReadOnlyCollection<MapPoint> Points => points.Values;

    public ImmutableArray<Segment> Segments => segments;

    public bool TryGetPoint(string id, [MaybeNullWhen(false)] out MapPoint point) =>
        points.TryGetValue(id, out point);

    public ImmutableArray<Edge> GetNeighbors(string pointId) =>
        adjacency.TryGetValue(pointId, out var edges) ? edges : ImmutableArray<Edge>.Empty;
}
