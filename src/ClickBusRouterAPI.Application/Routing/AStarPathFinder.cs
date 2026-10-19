using System.Collections.Immutable;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Application.Routing;

internal sealed record PathResult(ImmutableArray<string> NodeIds, double Cost);

/// <summary>
/// A* com múltiplas origens (as pontas do trecho onde o usuário está, cada uma com custo inicial)
/// e múltiplos destinos (ex.: todas as bilheterias). A heurística é a menor distância euclidiana
/// até qualquer destino: como o peso das arestas também é euclidiano, ela é admissível e consistente,
/// então o primeiro destino retirado da fila é o de menor custo real.
/// </summary>
internal static class AStarPathFinder
{
    public static PathResult? FindPath(
        IMapGraph graph,
        IReadOnlyList<(string NodeId, double InitialCost)> sources,
        IReadOnlyCollection<MapPoint> targets)
    {
        if (sources.Count == 0 || targets.Count == 0)
            return null;

        var targetIds = targets.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var targetPositions = targets.Select(t => t.Position).ToArray();

        var costSoFar = new Dictionary<string, double>(StringComparer.Ordinal);
        var cameFrom = new Dictionary<string, string>(StringComparer.Ordinal);
        var closed = new HashSet<string>(StringComparer.Ordinal);
        var open = new PriorityQueue<string, double>();

        foreach (var (nodeId, initialCost) in sources)
        {
            if (costSoFar.TryGetValue(nodeId, out var known) && known <= initialCost)
                continue;

            costSoFar[nodeId] = initialCost;
            open.Enqueue(nodeId, initialCost + Heuristic(graph, nodeId, targetPositions));
        }

        while (open.TryDequeue(out var current, out _))
        {
            if (!closed.Add(current))
                continue; // entrada obsoleta na fila

            var currentCost = costSoFar[current];

            if (targetIds.Contains(current))
                return new PathResult(Reconstruct(cameFrom, current), currentCost);

            foreach (var edge in graph.GetNeighbors(current))
            {
                if (closed.Contains(edge.ToId))
                    continue;

                var tentative = currentCost + edge.Length;
                if (costSoFar.TryGetValue(edge.ToId, out var existing) && tentative >= existing)
                    continue;

                costSoFar[edge.ToId] = tentative;
                cameFrom[edge.ToId] = current;
                open.Enqueue(edge.ToId, tentative + Heuristic(graph, edge.ToId, targetPositions));
            }
        }

        return null;
    }

    private static double Heuristic(IMapGraph graph, string nodeId, Coordinate[] targetPositions)
    {
        if (!graph.TryGetPoint(nodeId, out var point))
            return 0;

        var best = double.MaxValue;
        foreach (var target in targetPositions)
            best = Math.Min(best, point.Position.DistanceTo(target));

        return best;
    }

    private static ImmutableArray<string> Reconstruct(Dictionary<string, string> cameFrom, string last)
    {
        var path = new List<string> { last };
        while (cameFrom.TryGetValue(path[^1], out var previous))
            path.Add(previous);

        path.Reverse();
        return path.ToImmutableArray();
    }
}
