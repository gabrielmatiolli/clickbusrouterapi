using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using ClickBusRouterAPI.Application.Abstractions;
using ClickBusRouterAPI.Application.Common;
using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Application.Exceptions;
using ClickBusRouterAPI.Application.Options;
using ClickBusRouterAPI.Application.Routing;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Application.Services;

public sealed class RouteService(IMapGraph graph, IOptions<RoutingOptions> options) : IRouteService
{
    // Abaixo disso a projeção é tratada como "em cima" do ponto, para a rota começar nele.
    private const double EndpointTolerance = 1e-6;

    private readonly RoutingOptions _options = options.Value;

    public RouteResult CalculateFastestRoute(RouteQuery query)
    {
        Validate(query);

        var floor = string.IsNullOrWhiteSpace(query.Floor) ? _options.DefaultFloor : query.Floor.Trim();
        var origin = new Coordinate(query.X, query.Y);
        var targets = ResolveTargets(query);

        var snap = NetworkLocator.Snap(graph, origin, floor)
            ?? throw new RouteNotFoundException($"Não há corredores mapeados no andar '{floor}'.");

        if (snap.Distance > _options.MaxSnapDistance)
            throw new OriginOutOfMapException(
                $"A posição ({query.X}; {query.Y}) está a {snap.Distance:F2} unidades do corredor mais próximo " +
                $"(máximo permitido: {_options.MaxSnapDistance}).");

        var path = AStarPathFinder.FindPath(graph, BuildSources(snap), targets)
            ?? throw new RouteNotFoundException(
                $"Não existe caminho conectado até '{DescribeTargets(query, targets)}'.");

        return BuildResult(origin, snap, path);
    }

    private static void Validate(RouteQuery query)
    {
        if (!double.IsFinite(query.X) || !double.IsFinite(query.Y))
            throw new InvalidRouteRequestException("As coordenadas X e Y precisam ser números finitos.");

        var hasId = !string.IsNullOrWhiteSpace(query.DestinationId);
        var hasQuery = !string.IsNullOrWhiteSpace(query.DestinationQuery);

        if (hasId == hasQuery)
            throw new InvalidRouteRequestException(
                "Informe exatamente um destino: 'destinationId' ou 'destinationQuery'.");
    }

    private IReadOnlyCollection<MapPoint> ResolveTargets(RouteQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.DestinationId))
        {
            if (!graph.TryGetPoint(query.DestinationId.Trim(), out var point))
                throw new DestinationNotFoundException($"Ponto '{query.DestinationId}' não existe no mapa.");

            return [point];
        }

        var term = TextNormalizer.Normalize(query.DestinationQuery);
        var matches = graph.Points
            .Where(p => TextNormalizer.Normalize(p.Label).StartsWith(term, StringComparison.Ordinal))
            .ToList();

        return matches.Count > 0
            ? matches
            : throw new DestinationNotFoundException($"Nenhum ponto corresponde a '{query.DestinationQuery}'.");
    }

    private static List<(string NodeId, double InitialCost)> BuildSources(SnapResult snap)
    {
        var (a, b) = (snap.Segment.A, snap.Segment.B);

        if (snap.T <= EndpointTolerance)
            return [(a.Id, 0d)];

        if (snap.T >= 1 - EndpointTolerance)
            return [(b.Id, 0d)];

        return
        [
            (a.Id, snap.Projection.DistanceTo(a.Position)),
            (b.Id, snap.Projection.DistanceTo(b.Position))
        ];
    }

    private RouteResult BuildResult(Coordinate origin, SnapResult snap, PathResult path)
    {
        var nodes = path.NodeIds.Select(id => graph.TryGetPoint(id, out var p) ? p : throw new InvalidOperationException(id))
            .ToImmutableArray();

        var steps = new List<RouteStepDto>(nodes.Length);
        var cumulative = snap.Distance;
        Coordinate? beforePrevious = origin.DistanceTo(snap.Projection) > EndpointTolerance ? origin : null;
        var previous = snap.Projection;

        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            var legDistance = previous.DistanceTo(node.Position) + (i == 0 ? snap.Distance : 0);
            cumulative += previous.DistanceTo(node.Position);

            var instruction = i == 0
                ? (legDistance <= EndpointTolerance ? $"Você está em {node.Label}" : $"Siga até {node.Label}")
                : BuildTurnInstruction(beforePrevious, nodes[i - 1], node);

            if (i == nodes.Length - 1)
                instruction += ". Você chegou ao destino.";

            steps.Add(new RouteStepDto
            {
                Order = i + 1,
                Point = node.ToDto(),
                DistanceFromPrevious = Round(legDistance),
                CumulativeDistance = Round(cumulative),
                Instruction = instruction
            });

            if (previous.DistanceTo(node.Position) > EndpointTolerance)
                beforePrevious = previous;
            previous = node.Position;
        }

        var polyline = new List<CoordinateDto> { origin.ToDto(), snap.Projection.ToDto() };
        polyline.AddRange(nodes.Select(n => n.Position.ToDto()));

        var total = snap.Distance + path.Cost;
        var meters = total * _options.MetersPerMapUnit;

        return new RouteResult
        {
            Origin = origin.ToDto(),
            EntryPoint = new EntryPointDto
            {
                Position = snap.Projection.ToDto(),
                DistanceFromOrigin = Round(snap.Distance),
                SegmentFromId = snap.Segment.A.Id,
                SegmentToId = snap.Segment.B.Id
            },
            Destination = nodes[^1].ToDto(),
            TotalDistance = Round(total),
            TotalDistanceMeters = Math.Round(meters, 1),
            EstimatedSeconds = (int)Math.Ceiling(meters / _options.WalkingSpeedMetersPerSecond),
            Steps = steps,
            Polyline = polyline
        };
    }

    private static string BuildTurnInstruction(Coordinate? beforePivot, MapPoint pivot, MapPoint next)
    {
        var turn = TurnDescriber.Describe(beforePivot, pivot.Position, next.Position);
        return turn is null
            ? $"Em {pivot.Label}, siga até {next.Label}"
            : $"{turn} em {pivot.Label} e siga até {next.Label}";
    }

    private static string DescribeTargets(RouteQuery query, IReadOnlyCollection<MapPoint> targets) =>
        targets.Count == 1 ? targets.First().Label : query.DestinationQuery ?? string.Empty;

    private static double Round(double value) => Math.Round(value, 2);
}
