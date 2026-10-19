using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Domain.Entities;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Application.Common;

internal static class PointMapper
{
    public static PointDto ToDto(this MapPoint p) => new()
    {
        Id = p.Id,
        Label = p.Label,
        Type = p.Type,
        Floor = p.Floor,
        X = p.Position.X,
        Y = p.Position.Y
    };

    public static CoordinateDto ToDto(this Coordinate c) => new()
    {
        X = Math.Round(c.X, 2),
        Y = Math.Round(c.Y, 2)
    };
}
