namespace ClickBusRouterAPI.Infrastructure.Map;

public sealed class MapDataOptions
{
    public const string SectionName = "MapData";

    /// <summary>Caminho do JSON. Se relativo, é resolvido a partir da pasta da aplicação.</summary>
    public string FilePath { get; set; } = "Data/tiete-map.json";
}
