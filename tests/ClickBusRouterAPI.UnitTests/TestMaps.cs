using System.Text;
using ClickBusRouterAPI.Application.Options;
using ClickBusRouterAPI.Application.Services;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Infrastructure.Map;

namespace ClickBusRouterAPI.UnitTests;

/// <summary>Grafos usados nos testes: o mapa real do Tietê e mapas pequenos montados na hora.</summary>
internal static class TestMaps
{
    public static readonly string RealMapPath = Path.Combine(AppContext.BaseDirectory, "Data", "tiete-map.json");

    private static readonly Lazy<IMapGraph> RealGraph = new(() =>
    {
        using var stream = File.OpenRead(RealMapPath);
        return JsonMapGraphLoader.Parse(stream);
    });

    public static IMapGraph Tiete => RealGraph.Value;

    public static IMapGraph FromJson(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return JsonMapGraphLoader.Parse(stream);
    }

    public static RouteService CreateRouteService(IMapGraph? graph = null, RoutingOptions? options = null) =>
        new(graph ?? Tiete, Microsoft.Extensions.Options.Options.Create(options ?? new RoutingOptions()));

    /// <summary>
    /// Mapa em "L" (y cresce para baixo):
    ///   A(0,0) ── B(10,0)
    ///              │
    ///             C(10,10) ── D(20,10)      E(50,50) isolado
    /// Mais uma diagonal A ── D (~22,36), mais curta que A→B→C→D (30).
    /// A→C: por B = 20; pela diagonal = 22,36 + 10 = 32,36.
    /// </summary>
    public const string SmallMap = """
    {
      "points": [
        { "id": "A", "label": "Entrada A", "floor": "terreo", "type": "entrada",  "x": 0,  "y": 0 },
        { "id": "B", "label": "Corredor B", "floor": "terreo", "type": "corredor", "x": 10, "y": 0 },
        { "id": "C", "label": "Corredor C", "floor": "terreo", "type": "corredor", "x": 10, "y": 10 },
        { "id": "D", "label": "Bilheteria", "floor": "terreo", "type": "servico",  "x": 20, "y": 10 },
        { "id": "E", "label": "Bilheteria 2", "floor": "terreo", "type": "servico", "x": 50, "y": 50 }
      ],
      "connections": [
        { "from": "A", "to": "B" },
        { "from": "B", "to": "C" },
        { "from": "C", "to": "D" },
        { "from": "A", "to": "D" }
      ]
    }
    """;
}
