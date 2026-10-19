using System.Net;
using System.Net.Http.Json;
using ClickBusRouterAPI.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ClickBusRouterAPI.IntegrationTests;

public sealed class RoutesEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_retorna_rota_ate_o_destino()
    {
        var response = await _client.PostAsJsonAsync("/api/routes",
            new { x = 51.81, y = 75.81, destinationId = "TIETE-TER-SERVICO-031" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var route = await response.Content.ReadFromJsonAsync<RouteResult>(ApiJson.Options);
        Assert.NotNull(route);
        Assert.Equal("TIETE-TER-SERVICO-031", route.Destination.Id);
        Assert.Equal("TIETE-TER-ENTRADA-002", route.Steps[0].Point.Id);
        Assert.Equal(60.05, route.TotalDistance, precision: 2);
        Assert.True(route.EstimatedSeconds > 0);
    }

    [Fact]
    public async Task Post_com_busca_por_nome()
    {
        var response = await _client.PostAsJsonAsync("/api/routes",
            new { x = 51.81, y = 75.81, destinationQuery = "bilheteria" });

        var route = await response.Content.ReadFromJsonAsync<RouteResult>(ApiJson.Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("bilheteria 6", route!.Destination.Label);
    }

    [Fact]
    public async Task Get_com_query_string()
    {
        var route = await _client.GetFromJsonAsync<RouteResult>("/api/routes?x=26&y=74.32&q=sanitarios", ApiJson.Options);

        Assert.NotNull(route);
        Assert.Equal("TIETE-TER-SERVICO-015", route.Destination.Id);
    }

    [Fact]
    public async Task Post_sem_coordenada_retorna_400()
    {
        var response = await _client.PostAsJsonAsync("/api/routes", new { y = 75.81, destinationId = "TIETE-TER-SERVICO-031" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("X", problem!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_sem_coordenada_retorna_400()
    {
        var response = await _client.GetAsync("/api/routes?y=10&to=TIETE-TER-SERVICO-031");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sem_destino_retorna_400_em_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/routes", new { x = 50, y = 50 });

        await AssertProblem(response, HttpStatusCode.BadRequest, "Requisição inválida");
    }

    [Fact]
    public async Task Destino_inexistente_retorna_404()
    {
        var response = await _client.PostAsJsonAsync("/api/routes", new { x = 50, y = 50, destinationId = "NAO-EXISTE" });

        await AssertProblem(response, HttpStatusCode.NotFound, "Destino não encontrado");
    }

    [Fact]
    public async Task Origem_fora_do_mapa_retorna_422()
    {
        var response = await _client.PostAsJsonAsync("/api/routes",
            new { x = 900, y = 900, destinationId = "TIETE-TER-SERVICO-031" });

        await AssertProblem(response, HttpStatusCode.UnprocessableEntity, "Origem fora do mapa");
    }

    [Fact]
    public async Task Destino_sem_caminho_retorna_422()
    {
        var response = await _client.GetAsync("/api/routes?x=51.81&y=75.81&to=TIETE-TER-SERVICO-018");

        await AssertProblem(response, HttpStatusCode.UnprocessableEntity, "Rota inexistente");
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string title)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(title, problem.Title);
        Assert.Equal((int)status, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
    }
}
