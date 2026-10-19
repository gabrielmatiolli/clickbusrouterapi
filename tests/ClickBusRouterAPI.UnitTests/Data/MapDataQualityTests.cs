using System.Text.RegularExpressions;
using ClickBusRouterAPI.Application.Common;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.UnitTests.Data;

/// <summary>
/// Protege o JSON do mapa contra regressões: nomes escritos errado ou repetidos quebram a busca
/// por nome (ex.: "bilheria 6" não aparecia em "bilheteria").
/// </summary>
public sealed partial class MapDataQualityTests
{
    // Vocabulário permitido para o nome base (sem o número final), já normalizado.
    private static readonly HashSet<string> KnownNames =
    [
        "acesso metro", "cruzeiro do sul", "estacionamento", "corredor",
        "bilheteria", "informacao", "alimentacao", "praca de alimentacao", "lojas",
        "sanitarios", "telefones", "farmacia", "taxi", "banho"
    ];

    [Fact]
    public void Todo_nome_usa_um_termo_conhecido()
    {
        var unknown = TestMaps.Tiete.Points
            .Where(p => !KnownNames.Contains(BaseName(p.Label)))
            .Select(p => $"{p.Id} = \"{p.Label}\"")
            .ToList();

        Assert.True(unknown.Count == 0, "Nomes fora do vocabulário (erro de digitação?): " + string.Join(", ", unknown));
    }

    [Fact]
    public void Nomes_sao_unicos_ignorando_acento_e_caixa()
    {
        var duplicated = TestMaps.Tiete.Points
            .GroupBy(p => TextNormalizer.Normalize(p.Label))
            .Where(g => g.Count() > 1)
            .Select(g => $"\"{g.Key}\" ({string.Join(", ", g.Select(p => p.Id))})")
            .ToList();

        Assert.True(duplicated.Count == 0, "Nomes repetidos: " + string.Join("; ", duplicated));
    }

    [Theory]
    [InlineData("TIETE-TER-SERVICO-008", "bilheteria 6")]
    [InlineData("TIETE-TER-SERVICO-010", "bilheteria 8")]
    [InlineData("TIETE-TER-SERVICO-032", "alimentacao 7")]
    [InlineData("TIETE-TER-SERVICO-027", "lojas 6")]
    [InlineData("TIETE-TER-SERVICO-030", "telefones 3")]
    [InlineData("TIETE-TER-CORREDOR-046", "Corredor 72")]
    public void Nomes_corrigidos(string id, string expectedLabel)
    {
        Assert.True(TestMaps.Tiete.TryGetPoint(id, out var point));
        Assert.Equal(expectedLabel, point.Label);
    }

    [Fact]
    public void Tipo_do_ponto_bate_com_o_prefixo_do_id()
    {
        foreach (var point in TestMaps.Tiete.Points)
        {
            var expected = point.Type switch
            {
                PointType.Entrada => "-ENTRADA-",
                PointType.Corredor => "-CORREDOR-",
                PointType.Servico => "-SERVICO-",
                _ => throw new Xunit.Sdk.XunitException($"{point.Id} tem tipo desconhecido")
            };

            Assert.Contains(expected, point.Id);
        }
    }

    [Fact]
    public void Coordenadas_dentro_da_planta()
    {
        Assert.All(TestMaps.Tiete.Points, p =>
        {
            Assert.InRange(p.Position.X, 0, 100);
            Assert.InRange(p.Position.Y, 0, 100);
        });
    }

    [Fact]
    public void Unico_ponto_isolado_e_o_banho()
    {
        // Documenta a lacuna conhecida do mapa. Se alguém ligar o banho, atualize este teste.
        var isolated = TestMaps.Tiete.Points
            .Where(p => TestMaps.Tiete.GetNeighbors(p.Id).IsEmpty)
            .Select(p => p.Id);

        Assert.Equal(new[] { "TIETE-TER-SERVICO-018" }, isolated);
    }

    private static string BaseName(string label) =>
        TrailingNumber().Replace(TextNormalizer.Normalize(label), string.Empty);

    [GeneratedRegex(@"\s+\d+$")]
    private static partial Regex TrailingNumber();
}
