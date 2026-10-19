using ClickBusRouterAPI.Application.Routing;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.UnitTests.Application;

public sealed class AStarPathFinderTests
{
    private static readonly IMapGraph Small = TestMaps.FromJson(TestMaps.SmallMap);

    [Fact]
    public void Prefere_o_caminho_mais_curto_mesmo_com_mais_paradas()
    {
        var path = AStarPathFinder.FindPath(Small, [("A", 0d)], [Point(Small, "C")]);

        Assert.NotNull(path);
        Assert.Equal(new[] { "A", "B", "C" }, path.NodeIds);
        Assert.Equal(20, path.Cost, precision: 6);
    }

    [Fact]
    public void Usa_a_diagonal_quando_ela_e_mais_curta()
    {
        var path = AStarPathFinder.FindPath(Small, [("A", 0d)], [Point(Small, "D")]);

        Assert.NotNull(path);
        Assert.Equal(new[] { "A", "D" }, path.NodeIds);
        Assert.Equal(Math.Sqrt(500), path.Cost, precision: 6);
    }

    [Fact]
    public void Com_varias_origens_considera_o_custo_inicial_de_cada_uma()
    {
        // Saindo de B custa 100, saindo de C custa 1: deve partir de C.
        var path = AStarPathFinder.FindPath(Small, [("B", 100d), ("C", 1d)], [Point(Small, "D")]);

        Assert.NotNull(path);
        Assert.Equal(new[] { "C", "D" }, path.NodeIds);
        Assert.Equal(11, path.Cost, precision: 6);
    }

    [Fact]
    public void Com_varios_destinos_para_no_mais_proximo_alcancavel()
    {
        // E é isolado; o A* precisa ignorá-lo e chegar em D.
        var path = AStarPathFinder.FindPath(Small, [("B", 0d)], [Point(Small, "E"), Point(Small, "D")]);

        Assert.NotNull(path);
        Assert.Equal("D", path.NodeIds[^1]);
    }

    [Fact]
    public void Origem_igual_ao_destino_tem_custo_zero()
    {
        var path = AStarPathFinder.FindPath(Small, [("B", 0d)], [Point(Small, "B")]);

        Assert.NotNull(path);
        Assert.Equal(new[] { "B" }, path.NodeIds);
        Assert.Equal(0d, path.Cost);
    }

    [Fact]
    public void Destino_isolado_retorna_null() =>
        Assert.Null(AStarPathFinder.FindPath(Small, [("A", 0d)], [Point(Small, "E")]));

    [Fact]
    public void Sem_origens_ou_destinos_retorna_null()
    {
        Assert.Null(AStarPathFinder.FindPath(Small, [], [Point(Small, "D")]));
        Assert.Null(AStarPathFinder.FindPath(Small, [("A", 0d)], []));
    }

    /// <summary>
    /// Compara o A* com um Dijkstra de referência para TODOS os pares de pontos do mapa real.
    /// Garante que a heurística não faz o A* devolver uma rota pior.
    /// </summary>
    [Fact]
    public void Custo_igual_ao_Dijkstra_para_todos_os_pares_do_mapa_real()
    {
        var graph = TestMaps.Tiete;
        var points = graph.Points.ToList();

        foreach (var source in points)
        {
            var reference = Dijkstra(graph, source.Id);

            foreach (var target in points)
            {
                var path = AStarPathFinder.FindPath(graph, [(source.Id, 0d)], [target]);

                if (!reference.TryGetValue(target.Id, out var expected))
                {
                    Assert.Null(path);
                    continue;
                }

                Assert.NotNull(path);
                Assert.Equal(expected, path.Cost, precision: 9);
                Assert.Equal(source.Id, path.NodeIds[0]);
                Assert.Equal(target.Id, path.NodeIds[^1]);
                Assert.Equal(path.Cost, PathLength(graph, path.NodeIds), precision: 9);
            }
        }
    }

    private static MapPoint Point(IMapGraph graph, string id) =>
        graph.TryGetPoint(id, out var p) ? p : throw new ArgumentException(id);

    private static double PathLength(IMapGraph graph, IReadOnlyList<string> ids)
    {
        var total = 0d;
        for (var i = 1; i < ids.Count; i++)
            total += graph.GetNeighbors(ids[i - 1]).Single(e => e.ToId == ids[i]).Length;
        return total;
    }

    private static Dictionary<string, double> Dijkstra(IMapGraph graph, string sourceId)
    {
        var dist = new Dictionary<string, double> { [sourceId] = 0 };
        var queue = new PriorityQueue<string, double>();
        queue.Enqueue(sourceId, 0);

        while (queue.TryDequeue(out var current, out var d))
        {
            if (d > dist[current])
                continue;

            foreach (var edge in graph.GetNeighbors(current))
            {
                var candidate = d + edge.Length;
                if (!dist.TryGetValue(edge.ToId, out var known) || candidate < known)
                {
                    dist[edge.ToId] = candidate;
                    queue.Enqueue(edge.ToId, candidate);
                }
            }
        }

        return dist;
    }
}
