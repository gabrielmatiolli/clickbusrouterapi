using System.Text.Json;

namespace ClickBusRouterAPI.IntegrationTests;

/// <summary>Garante que a documentação OpenAPI é gerada com os parâmetros e tipos descritos.</summary>
public sealed class SwaggerDocumentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<JsonElement> GetDocument()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task PointType_lista_os_valores_como_texto()
    {
        var schema = (await GetDocument()).GetProperty("components").GetProperty("schemas").GetProperty("PointType");

        var values = schema.GetProperty("enum").EnumerateArray().Select(v => v.GetString()).ToList();
        Assert.Equal(new[] { "desconhecido", "entrada", "corredor", "servico" }, values);

        var description = schema.GetProperty("description").GetString();
        Assert.Contains("`servico`", description);
        Assert.Contains("bilheteria", description);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("destinationId")]
    [InlineData("destinationQuery")]
    [InlineData("floor")]
    public async Task Corpo_do_POST_documenta_cada_campo(string property)
    {
        var schema = (await GetDocument()).GetProperty("components").GetProperty("schemas").GetProperty("CalculateRouteRequest");

        var field = schema.GetProperty("properties").GetProperty(property);
        Assert.False(string.IsNullOrWhiteSpace(field.GetProperty("description").GetString()));
    }

    [Fact]
    public async Task X_e_Y_sao_obrigatorios_no_POST()
    {
        var schema = (await GetDocument()).GetProperty("components").GetProperty("schemas").GetProperty("CalculateRouteRequest");

        var required = schema.GetProperty("required").EnumerateArray().Select(v => v.GetString()).ToList();
        Assert.Contains("x", required);
        Assert.Contains("y", required);
    }

    [Theory]
    [InlineData("/api/routes", "get", "x")]
    [InlineData("/api/routes", "get", "y")]
    [InlineData("/api/routes", "get", "to")]
    [InlineData("/api/routes", "get", "q")]
    [InlineData("/api/routes", "get", "floor")]
    [InlineData("/api/points", "get", "type")]
    [InlineData("/api/points", "get", "search")]
    [InlineData("/api/points/{id}", "get", "id")]
    public async Task Parametros_de_query_e_rota_tem_descricao(string path, string method, string name)
    {
        var operation = (await GetDocument()).GetProperty("paths").GetProperty(path).GetProperty(method);

        var parameter = operation.GetProperty("parameters").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == name);

        Assert.False(string.IsNullOrWhiteSpace(parameter.GetProperty("description").GetString()));
    }

    [Fact]
    public async Task Rotas_documentam_os_codigos_de_erro()
    {
        var responses = (await GetDocument()).GetProperty("paths").GetProperty("/api/routes").GetProperty("post").GetProperty("responses");

        foreach (var code in new[] { "200", "400", "404", "422" })
            Assert.False(string.IsNullOrWhiteSpace(responses.GetProperty(code).GetProperty("description").GetString()));
    }
}
