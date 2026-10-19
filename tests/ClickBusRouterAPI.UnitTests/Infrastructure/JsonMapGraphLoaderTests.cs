using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.UnitTests.Infrastructure;

public sealed class JsonMapGraphLoaderTests
{
    [Fact]
    public void Carrega_o_mapa_real_completo()
    {
        var graph = TestMaps.Tiete;

        Assert.Equal(69, graph.Points.Count);
        Assert.Equal(89, graph.Segments.Length);
    }

    [Fact]
    public void Lookup_por_id_ignora_maiusculas()
    {
        Assert.True(TestMaps.Tiete.TryGetPoint("tiete-ter-servico-001", out var point));
        Assert.Equal("TIETE-TER-SERVICO-001", point.Id);
        Assert.Equal("bilheteria", point.Label);
    }

    [Fact]
    public void Converte_tipo_e_coordenadas()
    {
        Assert.True(TestMaps.Tiete.TryGetPoint("TIETE-TER-ENTRADA-002", out var point));

        Assert.Equal(PointType.Entrada, point.Type);
        Assert.Equal("terreo", point.Floor);
        Assert.Equal(51.81, point.Position.X);
        Assert.Equal(75.81, point.Position.Y);
    }

    [Fact]
    public void Conexoes_sao_bidirecionais_com_peso_euclidiano()
    {
        var graph = TestMaps.FromJson(TestMaps.SmallMap);

        var ab = Assert.Single(graph.GetNeighbors("A"), e => e.ToId == "B");
        var ba = Assert.Single(graph.GetNeighbors("B"), e => e.ToId == "A");
        var ad = Assert.Single(graph.GetNeighbors("A"), e => e.ToId == "D");

        Assert.Equal(10, ab.Length, precision: 6);
        Assert.Equal(ab.Length, ba.Length);
        Assert.Equal(Math.Sqrt(500), ad.Length, precision: 6);
    }

    [Fact]
    public void Ponto_sem_conexao_nao_tem_vizinhos()
    {
        var graph = TestMaps.FromJson(TestMaps.SmallMap);

        Assert.True(graph.GetNeighbors("E").IsEmpty);
        Assert.True(graph.GetNeighbors("NAO-EXISTE").IsEmpty);
    }

    [Fact]
    public void Conexao_repetida_nos_dois_sentidos_nao_duplica_aresta()
    {
        var graph = TestMaps.FromJson("""
        {
          "points": [
            { "id": "A", "label": "a", "floor": "terreo", "type": "corredor", "x": 0, "y": 0 },
            { "id": "B", "label": "b", "floor": "terreo", "type": "corredor", "x": 1, "y": 0 }
          ],
          "connections": [ { "from": "A", "to": "B" }, { "from": "B", "to": "A" }, { "from": "a", "to": "b" } ]
        }
        """);

        Assert.Single(graph.Segments);
        Assert.Single(graph.GetNeighbors("A"));
        Assert.Single(graph.GetNeighbors("B"));
    }

    [Fact]
    public void Conexao_para_o_proprio_ponto_e_ignorada()
    {
        var graph = TestMaps.FromJson("""
        {
          "points": [ { "id": "A", "label": "a", "floor": "terreo", "type": "corredor", "x": 0, "y": 0 } ],
          "connections": [ { "from": "A", "to": "A" } ]
        }
        """);

        Assert.Empty(graph.Segments);
    }

    [Fact]
    public void Tipo_desconhecido_vira_Desconhecido()
    {
        var graph = TestMaps.FromJson("""
        {
          "points": [ { "id": "A", "label": "a", "floor": "terreo", "type": "elevador", "x": 0, "y": 0 } ],
          "connections": []
        }
        """);

        Assert.True(graph.TryGetPoint("A", out var point));
        Assert.Equal(PointType.Desconhecido, point.Type);
    }

    [Fact]
    public void Id_duplicado_falha_na_carga()
    {
        var ex = Assert.Throws<InvalidDataException>(() => TestMaps.FromJson("""
        {
          "points": [
            { "id": "A", "label": "a", "floor": "terreo", "type": "corredor", "x": 0, "y": 0 },
            { "id": "a", "label": "b", "floor": "terreo", "type": "corredor", "x": 1, "y": 1 }
          ],
          "connections": []
        }
        """));

        Assert.Contains("duplicado", ex.Message);
    }

    [Fact]
    public void Conexao_para_ponto_inexistente_falha_na_carga()
    {
        var ex = Assert.Throws<InvalidDataException>(() => TestMaps.FromJson("""
        {
          "points": [ { "id": "A", "label": "a", "floor": "terreo", "type": "corredor", "x": 0, "y": 0 } ],
          "connections": [ { "from": "A", "to": "Z" } ]
        }
        """));

        Assert.Contains("inexistente", ex.Message);
    }

    [Fact]
    public void Ponto_sem_id_falha_na_carga() =>
        Assert.Throws<InvalidDataException>(() => TestMaps.FromJson("""
        { "points": [ { "id": " ", "label": "a", "floor": "terreo", "type": "corredor", "x": 0, "y": 0 } ], "connections": [] }
        """));
}
