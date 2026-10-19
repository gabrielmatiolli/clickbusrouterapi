namespace ClickBusRouterAPI.Infrastructure.Map;

// Espelham o formato do arquivo; não vazam para fora da camada.
internal sealed class MapDocumentJson
{
    public List<MapPointJson> Points { get; init; } = [];
    public List<MapConnectionJson> Connections { get; init; } = [];
}

internal sealed class MapPointJson
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Floor { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
}

internal sealed class MapConnectionJson
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
}
