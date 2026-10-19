using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Application.Exceptions;
using ClickBusRouterAPI.Application.Options;

namespace ClickBusRouterAPI.UnitTests.Application;

public sealed class RouteServiceTests
{
    private const double MetroX = 51.81, MetroY = 75.81;

    private readonly ClickBusRouterAPI.Application.Services.RouteService _tiete = TestMaps.CreateRouteService();
    private readonly ClickBusRouterAPI.Application.Services.RouteService _small =
        TestMaps.CreateRouteService(TestMaps.FromJson(TestMaps.SmallMap));

    // ---------- mapa real ----------

    [Fact]
    public void Do_metro_ate_a_praca_de_alimentacao()
    {
        var result = _tiete.CalculateFastestRoute(new RouteQuery(MetroX, MetroY, DestinationId: "TIETE-TER-SERVICO-031"));

        Assert.Equal(
            new[]
            {
                "TIETE-TER-ENTRADA-002", "TIETE-TER-CORREDOR-047", "TIETE-TER-CORREDOR-060", "TIETE-TER-CORREDOR-044",
                "TIETE-TER-CORREDOR-022", "TIETE-TER-CORREDOR-054", "TIETE-TER-CORREDOR-041", "TIETE-TER-SERVICO-031"
            },
            result.Steps.Select(s => s.Point.Id));
        Assert.Equal(60.05, result.TotalDistance, precision: 2);
        Assert.Equal("Você está em Acesso metro", result.Steps[0].Instruction);
        Assert.EndsWith("Você chegou ao destino.", result.Steps[^1].Instruction);
    }

    [Fact]
    public void Busca_por_nome_encontra_a_bilheteria_que_antes_estava_escrita_errado()
    {
        // "bilheteria 6" era "bilheria 6" no JSON original e ficava fora da busca;
        // a rota ia para a "bilheteria 10" (59,45). Agora a mais próxima é a 6.
        var result = _tiete.CalculateFastestRoute(new RouteQuery(MetroX, MetroY, DestinationQuery: "Bilheteria"));

        Assert.Equal("TIETE-TER-SERVICO-008", result.Destination.Id);
        Assert.Equal("bilheteria 6", result.Destination.Label);
        Assert.Equal(58.97, result.TotalDistance, precision: 2);
    }

    [Fact]
    public void Busca_por_alimentacao_inclui_o_ponto_corrigido()
    {
        var result = _tiete.CalculateFastestRoute(new RouteQuery(MetroX, MetroY, DestinationQuery: "alimentação"));

        Assert.Equal("TIETE-TER-SERVICO-032", result.Destination.Id); // "alimentacao 7"
        Assert.Equal(14.13, result.TotalDistance, precision: 2);
    }

    [Theory]
    [InlineData("sanitarios", "TIETE-TER-SERVICO-015")]
    [InlineData("SANITÁRIOS", "TIETE-TER-SERVICO-015")]
    [InlineData("praça", "TIETE-TER-SERVICO-031")]
    public void Busca_ignora_acento_e_caixa(string query, string expectedId)
    {
        var result = _tiete.CalculateFastestRoute(new RouteQuery(26, 74.32, DestinationQuery: query));

        Assert.Equal(expectedId, result.Destination.Id);
    }

    [Fact]
    public void Usuario_ja_no_destino()
    {
        var result = _tiete.CalculateFastestRoute(new RouteQuery(41.65, 29.48, DestinationQuery: "bilheteria"));

        var step = Assert.Single(result.Steps);
        Assert.Equal("TIETE-TER-SERVICO-008", step.Point.Id);
        Assert.Equal(0d, result.TotalDistance);
        Assert.Equal(0, result.EstimatedSeconds);
        Assert.Equal("Você está em bilheteria 6. Você chegou ao destino.", step.Instruction);
    }

    [Fact]
    public void Usuario_no_meio_do_corredor_e_projetado_no_trecho()
    {
        // Meio do trecho "Acesso metro" — "Corredor 82", deslocado ~1 unidade para o lado.
        var result = _tiete.CalculateFastestRoute(new RouteQuery(52.84, 72.23, DestinationId: "TIETE-TER-CORREDOR-060"));

        Assert.Equal(1.0, result.EntryPoint.DistanceFromOrigin, precision: 2);
        Assert.Equal("TIETE-TER-CORREDOR-047", result.Steps[0].Point.Id);
        Assert.Equal(21.60, result.TotalDistance, precision: 2);
    }

    [Fact]
    public void Destino_sem_conexao_retorna_rota_inexistente() =>
        Assert.Throws<RouteNotFoundException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(MetroX, MetroY, DestinationId: "TIETE-TER-SERVICO-018")));

    // ---------- consistência da resposta ----------

    [Fact]
    public void Distancias_acumuladas_e_polyline_sao_consistentes()
    {
        var result = _tiete.CalculateFastestRoute(new RouteQuery(45.51, 82.41, DestinationQuery: "telefones"));

        Assert.Equal(result.TotalDistance, result.Steps[^1].CumulativeDistance, precision: 2);
        // Cada trecho é arredondado em 2 casas, então a soma pode diferir alguns centésimos.
        Assert.InRange(Math.Abs(result.TotalDistance - result.Steps.Sum(s => s.DistanceFromPrevious)), 0, 0.01 * result.Steps.Count);
        Assert.Equal(Enumerable.Range(1, result.Steps.Count), result.Steps.Select(s => s.Order));
        Assert.True(result.Steps.Zip(result.Steps.Skip(1)).All(p => p.Second.CumulativeDistance >= p.First.CumulativeDistance));

        Assert.Equal(result.Steps.Count + 2, result.Polyline.Count);
        Assert.Equal(result.Origin, result.Polyline[0]);
        Assert.Equal(result.EntryPoint.Position, result.Polyline[1]);
        Assert.Equal(result.Destination.Id, result.Steps[^1].Point.Id);
    }

    [Fact]
    public void Metros_e_tempo_usam_as_opcoes()
    {
        var service = TestMaps.CreateRouteService(
            TestMaps.FromJson(TestMaps.SmallMap),
            new RoutingOptions { MetersPerMapUnit = 3, WalkingSpeedMetersPerSecond = 2 });

        var result = service.CalculateFastestRoute(new RouteQuery(5, -2, DestinationId: "C"));

        Assert.Equal(17, result.TotalDistance, precision: 6);      // 2 até o corredor + 5 até B + 10 até C
        Assert.Equal(51, result.TotalDistanceMeters, precision: 6); // 17 * 3
        Assert.Equal(26, result.EstimatedSeconds);                  // ceil(51 / 2)
    }

    // ---------- mapa pequeno ----------

    [Fact]
    public void Gera_instrucao_de_curva()
    {
        var result = _small.CalculateFastestRoute(new RouteQuery(5, -2, DestinationId: "C"));

        Assert.Equal(new[] { "B", "C" }, result.Steps.Select(s => s.Point.Id));
        Assert.Equal("Siga até Corredor B", result.Steps[0].Instruction);
        Assert.Equal("Vire à direita em Corredor B e siga até Corredor C. Você chegou ao destino.", result.Steps[1].Instruction);
        Assert.Equal(7, result.Steps[0].DistanceFromPrevious, precision: 6); // 2 até o corredor + 5
        Assert.Equal(("A", "B"), (result.EntryPoint.SegmentFromId, result.EntryPoint.SegmentToId));
    }

    [Fact]
    public void Ignora_destino_isolado_com_o_mesmo_nome()
    {
        var result = _small.CalculateFastestRoute(new RouteQuery(0, 0, DestinationQuery: "bilheteria"));

        Assert.Equal("D", result.Destination.Id);
    }

    // ---------- erros ----------

    [Fact]
    public void Origem_longe_demais_do_corredor() =>
        Assert.Throws<OriginOutOfMapException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(500, 500, DestinationId: "TIETE-TER-SERVICO-001")));

    [Fact]
    public void Limite_de_distancia_vem_das_opcoes()
    {
        var strict = TestMaps.CreateRouteService(TestMaps.FromJson(TestMaps.SmallMap), new RoutingOptions { MaxSnapDistance = 1 });

        Assert.Throws<OriginOutOfMapException>(() => strict.CalculateFastestRoute(new RouteQuery(5, -2, DestinationId: "C")));
    }

    [Fact]
    public void Andar_sem_mapa() =>
        Assert.Throws<RouteNotFoundException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(MetroX, MetroY, DestinationId: "TIETE-TER-SERVICO-001", Floor: "mezanino")));

    [Fact]
    public void Destino_por_id_desconhecido() =>
        Assert.Throws<DestinationNotFoundException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(50, 50, DestinationId: "NAO-EXISTE")));

    [Fact]
    public void Destino_por_nome_desconhecido() =>
        Assert.Throws<DestinationNotFoundException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(50, 50, DestinationQuery: "elevador")));

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "  ")]
    [InlineData("TIETE-TER-SERVICO-001", "bilheteria")]
    public void Exige_exatamente_um_destino(string? id, string? query) =>
        Assert.Throws<InvalidRouteRequestException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(50, 50, id, query)));

    [Theory]
    [InlineData(double.NaN, 10)]
    [InlineData(10, double.PositiveInfinity)]
    public void Rejeita_coordenada_invalida(double x, double y) =>
        Assert.Throws<InvalidRouteRequestException>(() =>
            _tiete.CalculateFastestRoute(new RouteQuery(x, y, DestinationId: "TIETE-TER-SERVICO-001")));
}
