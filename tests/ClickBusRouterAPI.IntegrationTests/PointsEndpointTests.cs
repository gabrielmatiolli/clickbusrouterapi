using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.IntegrationTests;

public sealed class PointsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("servico", PointType.Servico, 32)]
    [InlineData("SERVICO", PointType.Servico, 32)]
    [InlineData("entrada", PointType.Entrada, 3)]
    [InlineData("corredor", PointType.Corredor, 34)]
    public async Task Filtra_por_tipo(string query, PointType expected, int count)
    {
        var points = await _client.GetFromJsonAsync<List<PointDto>>($"/api/points?type={query}", ApiJson.Options);

        Assert.NotNull(points);
        Assert.Equal(count, points.Count);
        Assert.All(points, p => Assert.Equal(expected, p.Type));
    }

    [Fact]
    public async Task Tipo_invalido_retorna_400()
    {
        var response = await _client.GetAsync("/api/points?type=elevador");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Tipo_e_serializado_como_texto()
    {
        var json = await _client.GetStringAsync("/api/points/TIETE-TER-ENTRADA-002");

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("entrada", doc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Busca_por_nome()
    {
        var points = await _client.GetFromJsonAsync<List<PointDto>>("/api/points?search=bilheteria", ApiJson.Options);

        Assert.Equal(10, points!.Count);
    }

    [Fact]
    public async Task Ponto_por_id()
    {
        var point = await _client.GetFromJsonAsync<PointDto>("/api/points/TIETE-TER-SERVICO-008", ApiJson.Options);

        Assert.Equal("bilheteria 6", point!.Label);
        Assert.Equal(PointType.Servico, point.Type);
    }

    [Fact]
    public async Task Ponto_inexistente_retorna_404()
    {
        var response = await _client.GetAsync("/api/points/NAO-EXISTE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_check()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
