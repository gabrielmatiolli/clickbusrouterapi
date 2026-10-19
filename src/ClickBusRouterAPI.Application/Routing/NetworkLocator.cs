using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Application.Routing;

internal sealed record SnapResult(Segment Segment, Coordinate Projection, double T, double Distance);

/// <summary>
/// Projeta a posição do usuário no trecho de corredor mais próximo. É mais preciso do que
/// pegar o nó mais próximo, porque o usuário costuma estar no meio de um corredor.
/// </summary>
internal static class NetworkLocator
{
    public static SnapResult? Snap(IMapGraph graph, Coordinate position, string floor)
    {
        SnapResult? best = null;

        foreach (var segment in graph.Segments)
        {
            if (!IsOnFloor(segment.A, floor) || !IsOnFloor(segment.B, floor))
                continue;

            var projection = Project(position, segment.A.Position, segment.B.Position, out var t);
            var distance = position.DistanceTo(projection);

            if (best is null || distance < best.Distance)
                best = new SnapResult(segment, projection, t, distance);
        }

        return best;
    }

    private static bool IsOnFloor(MapPoint point, string floor) =>
        string.Equals(point.Floor, floor, StringComparison.OrdinalIgnoreCase);

    private static Coordinate Project(Coordinate p, Coordinate a, Coordinate b, out double t)
    {
        var abX = b.X - a.X;
        var abY = b.Y - a.Y;
        var lengthSquared = abX * abX + abY * abY;

        t = lengthSquared == 0
            ? 0
            : Math.Clamp(((p.X - a.X) * abX + (p.Y - a.Y) * abY) / lengthSquared, 0, 1);

        return new Coordinate(a.X + t * abX, a.Y + t * abY);
    }
}
