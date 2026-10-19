using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClickBusRouterAPI.IntegrationTests;

/// <summary>Sobe a API em memória apontando para o JSON do próprio projeto Api.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var mapPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "Data", "tiete-map.json");
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MapData:FilePath"] = mapPath,
                ["Swagger:Enabled"] = "true"
            });
        });
    }
}
