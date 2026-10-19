namespace ClickBusRouterAPI.Application.Options;

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    /// <summary>Andar usado quando a requisição não informa um.</summary>
    public string DefaultFloor { get; set; } = "terreo";

    /// <summary>
    /// Distância máxima (em unidades do mapa) entre o usuário e o corredor mais próximo.
    /// Acima disso, consideramos que ele está fora da área mapeada.
    /// </summary>
    public double MaxSnapDistance { get; set; } = 10;

    /// <summary>Fator de conversão de unidade do mapa para metros. Calibre com a planta real.</summary>
    public double MetersPerMapUnit { get; set; } = 2.0;

    /// <summary>Velocidade média de caminhada usada na estimativa de tempo.</summary>
    public double WalkingSpeedMetersPerSecond { get; set; } = 1.3;
}
