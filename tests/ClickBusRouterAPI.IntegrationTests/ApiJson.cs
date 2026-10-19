using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClickBusRouterAPI.IntegrationTests;

/// <summary>Mesmas opções de JSON da API (enums como texto camelCase).</summary>
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };
}
