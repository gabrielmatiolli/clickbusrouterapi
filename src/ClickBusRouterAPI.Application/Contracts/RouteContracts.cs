using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.Application.Contracts;

/// <summary>
/// Pedido de rota usado internamente pela camada de aplicação.
/// Informe <see cref="DestinationId"/> para um ponto exato ou <see cref="DestinationQuery"/>
/// (ex.: "bilheteria", "sanitarios") para ir ao mais próximo com aquele nome.
/// </summary>
/// <param name="X">Coordenada X do usuário no sistema do mapa.</param>
/// <param name="Y">Coordenada Y do usuário no sistema do mapa (cresce para baixo).</param>
/// <param name="DestinationId">Id exato do ponto de destino.</param>
/// <param name="DestinationQuery">Início do nome do destino; escolhe o mais próximo pelo caminho.</param>
/// <param name="Floor">Andar do usuário. Nulo usa o andar padrão configurado.</param>
public sealed record RouteQuery(
    double X,
    double Y,
    string? DestinationId = null,
    string? DestinationQuery = null,
    string? Floor = null);

/// <summary>Posição no plano do mapa, arredondada em 2 casas decimais.</summary>
public sealed record CoordinateDto
{
    /// <summary>Eixo horizontal, da esquerda (0) para a direita (100).</summary>
    /// <example>51.81</example>
    public required double X { get; init; }

    /// <summary>Eixo vertical, de cima (0) para baixo (100).</summary>
    /// <example>75.81</example>
    public required double Y { get; init; }
}

/// <summary>Ponto do mapa (entrada, corredor ou serviço).</summary>
public sealed record PointDto
{
    /// <summary>
    /// Identificador único no formato <c>TIETE-{ANDAR}-{TIPO}-{NNN}</c>.
    /// É o valor aceito em <c>destinationId</c>.
    /// </summary>
    /// <example>TIETE-TER-SERVICO-031</example>
    public required string Id { get; init; }

    /// <summary>Nome exibido para o usuário. É o texto pesquisado por <c>destinationQuery</c>.</summary>
    /// <example>praça de alimentação</example>
    public required string Label { get; init; }

    /// <summary>Categoria do ponto: <c>entrada</c>, <c>corredor</c> ou <c>servico</c>.</summary>
    /// <example>servico</example>
    public required PointType Type { get; init; }

    /// <summary>Andar onde o ponto fica. Valor existente hoje: <c>terreo</c>.</summary>
    /// <example>terreo</example>
    public required string Floor { get; init; }

    /// <summary>Coordenada X do ponto (0–100).</summary>
    /// <example>53.27</example>
    public required double X { get; init; }

    /// <summary>Coordenada Y do ponto (0–100, cresce para baixo).</summary>
    /// <example>16.11</example>
    public required double Y { get; init; }
}

/// <summary>Onde a posição do usuário foi encaixada na malha de corredores.</summary>
public sealed record EntryPointDto
{
    /// <summary>Ponto do corredor mais próximo da origem (projeção perpendicular sobre o trecho).</summary>
    public required CoordinateDto Position { get; init; }

    /// <summary>Distância, em unidades do mapa, entre a origem informada e <see cref="Position"/>.</summary>
    /// <example>0.99</example>
    public required double DistanceFromOrigin { get; init; }

    /// <summary>Id de uma das pontas do trecho em que o usuário foi encaixado.</summary>
    /// <example>TIETE-TER-ENTRADA-002</example>
    public required string SegmentFromId { get; init; }

    /// <summary>Id da outra ponta do trecho em que o usuário foi encaixado.</summary>
    /// <example>TIETE-TER-CORREDOR-047</example>
    public required string SegmentToId { get; init; }
}

/// <summary>Um passo da rota: chegar até <see cref="Point"/>.</summary>
public sealed record RouteStepDto
{
    /// <summary>Posição do passo na rota, começando em 1.</summary>
    /// <example>1</example>
    public required int Order { get; init; }

    /// <summary>Ponto alcançado neste passo.</summary>
    public required PointDto Point { get; init; }

    /// <summary>
    /// Distância em unidades do mapa desde o passo anterior. No primeiro passo inclui
    /// o trecho da origem até o corredor.
    /// </summary>
    /// <example>8.71</example>
    public required double DistanceFromPrevious { get; init; }

    /// <summary>Distância acumulada desde a origem até este ponto, em unidades do mapa.</summary>
    /// <example>8.71</example>
    public required double CumulativeDistance { get; init; }

    /// <summary>
    /// Instrução em português. Curvas possíveis: "Siga em frente", "Mantenha-se à direita/esquerda",
    /// "Vire à direita/esquerda" e "Retorne". O último passo termina com "Você chegou ao destino.".
    /// </summary>
    /// <example>Vire à direita em Corredor 89 e siga até Corredor 71</example>
    public required string Instruction { get; init; }
}

/// <summary>Rota calculada da origem até o destino.</summary>
public sealed record RouteResult
{
    /// <summary>Coordenada de origem, como foi enviada.</summary>
    public required CoordinateDto Origin { get; init; }

    /// <summary>Onde a origem foi encaixada na malha de corredores.</summary>
    public required EntryPointDto EntryPoint { get; init; }

    /// <summary>Destino escolhido. Com <c>destinationQuery</c>, é o candidato mais próximo pelo caminho.</summary>
    public required PointDto Destination { get; init; }

    /// <summary>Distância total da rota em unidades do mapa (inclui o trecho até o corredor).</summary>
    /// <example>60.05</example>
    public required double TotalDistance { get; init; }

    /// <summary>
    /// Distância total em metros: <c>TotalDistance × Routing:MetersPerMapUnit</c>.
    /// O fator é configurável e precisa ser calibrado com a planta real.
    /// </summary>
    /// <example>120.1</example>
    public required double TotalDistanceMeters { get; init; }

    /// <summary>Tempo estimado a pé em segundos: <c>TotalDistanceMeters ÷ Routing:WalkingSpeedMetersPerSecond</c>, arredondado para cima.</summary>
    /// <example>93</example>
    public required int EstimatedSeconds { get; init; }

    /// <summary>Passos em ordem, do primeiro ponto da malha até o destino.</summary>
    public required IReadOnlyList<RouteStepDto> Steps { get; init; }

    /// <summary>
    /// Pontos para desenhar a rota no mapa, em ordem: origem, ponto de entrada na malha
    /// e depois cada ponto de <see cref="Steps"/>. Sempre tem <c>Steps.Count + 2</c> itens.
    /// </summary>
    public required IReadOnlyList<CoordinateDto> Polyline { get; init; }
}
