using ClickBusRouterAPI.Application.Routing;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.UnitTests.Application;

/// <summary>Lembrete: no mapa o eixo Y cresce para baixo (coordenadas de tela).</summary>
public sealed class TurnDescriberTests
{
    private static readonly Coordinate West = new(0, 10);
    private static readonly Coordinate Pivot = new(10, 10);

    [Theory]
    [InlineData(20, 10, "Siga em frente")]          // continua para leste
    [InlineData(10, 20, "Vire à direita")]          // leste -> sul (para baixo na tela)
    [InlineData(10, 0, "Vire à esquerda")]          // leste -> norte (para cima na tela)
    [InlineData(20, 15, "Mantenha-se à direita")]   // ~27° para baixo
    [InlineData(20, 5, "Mantenha-se à esquerda")]   // ~27° para cima
    [InlineData(0, 10.5, "Retorne")]                // volta quase pelo mesmo caminho
    public void Classifica_a_curva(double nextX, double nextY, string expected) =>
        Assert.Equal(expected, TurnDescriber.Describe(West, Pivot, new Coordinate(nextX, nextY)));

    [Fact]
    public void Sem_direcao_de_chegada_nao_ha_curva()
    {
        Assert.Null(TurnDescriber.Describe(null, Pivot, new Coordinate(20, 10)));
        Assert.Null(TurnDescriber.Describe(Pivot, Pivot, new Coordinate(20, 10)));
        Assert.Null(TurnDescriber.Describe(West, Pivot, Pivot));
    }
}
