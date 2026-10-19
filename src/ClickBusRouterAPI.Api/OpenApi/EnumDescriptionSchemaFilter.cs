using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClickBusRouterAPI.Api.OpenApi;

/// <summary>
/// Acrescenta à descrição de cada enum a lista de valores aceitos, exatamente como trafegam no JSON
/// (camelCase) e com o texto do <see cref="DescriptionAttribute"/> de cada membro.
/// </summary>
internal sealed class EnumDescriptionSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Type) ?? context.Type;
        if (!type.IsEnum)
            return;

        var values = Enum.GetNames(type).Select(name =>
        {
            var wireName = JsonNamingPolicy.CamelCase.ConvertName(name);
            var description = type.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            return description is null ? $"- `{wireName}`" : $"- `{wireName}`: {description}";
        });

        var header = string.IsNullOrWhiteSpace(schema.Description) ? string.Empty : schema.Description.Trim() + "\n\n";
        schema.Description = header + "Valores aceitos:\n\n" + string.Join("\n", values);
    }
}
