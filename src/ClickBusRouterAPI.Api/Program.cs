using System.Text.Json;
using System.Text.Json.Serialization;
using ClickBusRouterAPI.Api.ErrorHandling;
using ClickBusRouterAPI.Api.OpenApi;
using ClickBusRouterAPI.Application;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        // Enums trafegam como texto ("servico"), não como número. Também vale para o Swagger.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RoutingExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClickBusRouterAPI",
        Version = "v1",
        Description =
            "Calcula a rota a pé mais rápida dentro do Terminal Tietê.\n\n" +
            "**Sistema de coordenadas:** `x` e `y` vão de 0 a 100 sobre a planta; `x` cresce para a direita e " +
            "`y` cresce para baixo. Distâncias são devolvidas em unidades do mapa e em metros (fator configurável).\n\n" +
            "**Fluxo típico:** consulte `GET /api/points` para descobrir ids e nomes, depois chame `POST /api/routes` " +
            "com a posição do usuário e o destino."
    });

    // XML de todas as camadas: documenta controllers (Api), DTOs (Application) e enums (Domain).
    foreach (var xml in Directory.EnumerateFiles(AppContext.BaseDirectory, "ClickBusRouterAPI.*.xml"))
        c.IncludeXmlComments(xml, includeControllerXmlComments: true);

    c.SupportNonNullableReferenceTypes();
    c.SchemaFilter<EnumDescriptionSchemaFilter>();
});

var app = builder.Build();

// Carrega e congela o grafo no startup: JSON inválido derruba a aplicação já na subida,
// e o primeiro request não paga o custo de montar o FrozenDictionary.
app.Services.GetRequiredService<IMapGraph>();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "ClickBusRouterAPI");
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
