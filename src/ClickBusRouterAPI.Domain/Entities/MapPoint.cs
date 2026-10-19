using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Domain.Entities;

/// <summary>Nó do grafo: entrada, corredor ou serviço do terminal.</summary>
public sealed record MapPoint(
    string Id,
    string Label,
    string Floor,
    PointType Type,
    Coordinate Position);
