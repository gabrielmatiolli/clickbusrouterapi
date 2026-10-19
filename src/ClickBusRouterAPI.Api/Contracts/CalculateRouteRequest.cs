using System.ComponentModel.DataAnnotations;
using ClickBusRouterAPI.Application.Contracts;

namespace ClickBusRouterAPI.Api.Contracts;

/// <summary>
/// Posição atual do usuário e destino desejado.
/// Informe <b>exatamente um</b> destino: <c>destinationId</c> ou <c>destinationQuery</c>.
/// </summary>
public sealed class CalculateRouteRequest
{
    /// <summary>
    /// Coordenada horizontal do usuário, no mesmo sistema da planta do mapa:
    /// <c>0</c> = borda esquerda, <c>100</c> = borda direita.
    /// A posição é encaixada no corredor mais próximo; se ele estiver a mais de
    /// <c>Routing:MaxSnapDistance</c> unidades (padrão 10), a API responde <c>422</c>.
    /// </summary>
    /// <example>51.81</example>
    [Required, Range(-1000, 1000)]
    public double? X { get; init; }

    /// <summary>
    /// Coordenada vertical do usuário: <c>0</c> = topo da planta, <c>100</c> = base
    /// (o eixo cresce para baixo, como em coordenadas de tela).
    /// </summary>
    /// <example>75.81</example>
    [Required, Range(-1000, 1000)]
    public double? Y { get; init; }

    /// <summary>
    /// Id exato do ponto de destino, como retornado em <c>GET /api/points</c>
    /// (formato <c>TIETE-{ANDAR}-{TIPO}-{NNN}</c>, sem diferenciar maiúsculas).
    /// Não informe junto com <c>destinationQuery</c>.
    /// </summary>
    /// <example>TIETE-TER-SERVICO-031</example>
    [StringLength(64)]
    public string? DestinationId { get; init; }

    /// <summary>
    /// Início do nome do destino, sem diferenciar acentos ou maiúsculas. Todos os pontos cujo nome começa
    /// com esse texto viram candidatos e a rota vai para o <b>mais próximo pelo caminho</b>.
    /// Valores úteis: <c>bilheteria</c>, <c>informacao</c>, <c>alimentacao</c>, <c>praca de alimentacao</c>,
    /// <c>lojas</c>, <c>sanitarios</c>, <c>telefones</c>, <c>farmacia</c>, <c>taxi</c>,
    /// <c>acesso metro</c>, <c>cruzeiro do sul</c>, <c>estacionamento</c>.
    /// Também aceita o nome completo (ex.: <c>bilheteria 6</c>).
    /// Não informe junto com <c>destinationId</c>.
    /// </summary>
    /// <example>bilheteria</example>
    [StringLength(100)]
    public string? DestinationQuery { get; init; }

    /// <summary>
    /// Andar em que o usuário está. Opcional; quando omitido usa <c>Routing:DefaultFloor</c>.
    /// Valor disponível hoje: <c>terreo</c>.
    /// </summary>
    /// <example>terreo</example>
    [StringLength(32)]
    public string? Floor { get; init; }

    public RouteQuery ToQuery() => new(X!.Value, Y!.Value, DestinationId, DestinationQuery, Floor);
}
