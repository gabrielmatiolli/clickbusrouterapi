using ClickBusRouterAPI.Application.Routing;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.UnitTests.Application;

public sealed class NetworkLocatorTests
{
    private static readonly ClickBusRouterAPI.Domain.Abstractions.IMapGraph Graph = TestMaps.FromJson(TestMaps.SmallMap);

    [Fact]
    public void Projeta_no_meio_do_trecho_mais_proximo()
    {
        var snap = NetworkLocator.Snap(Graph, new Coordinate(5, -2), "terreo");

        Assert.NotNull(snap);
        Assert.Equal(("A", "B"), (snap.Segment.A.Id, snap.Segment.B.Id));
        Assert.Equal(new Coordinate(5, 0), snap.Projection);
        Assert.Equal(0.5, snap.T, precision: 6);
        Assert.Equal(2, snap.Distance, precision: 6);
    }

    [Fact]
    public void Projecao_fica_presa_na_ponta_do_trecho()
    {
        var snap = NetworkLocator.Snap(Graph, new Coordinate(-5, -1), "terreo");

        Assert.NotNull(snap);
        Assert.Equal(new Coordinate(0, 0), snap.Projection);
        Assert.Equal(Math.Sqrt(26), snap.Distance, precision: 6);
    }

    [Fact]
    public void Andar_sem_corredores_retorna_null() =>
        Assert.Null(NetworkLocator.Snap(Graph, new Coordinate(5, -2), "mezanino"));

    [Fact]
    public void Andar_ignora_maiusculas() =>
        Assert.NotNull(NetworkLocator.Snap(Graph, new Coordinate(5, -2), "TERREO"));
}
