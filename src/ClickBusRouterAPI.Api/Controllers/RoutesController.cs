using System.ComponentModel.DataAnnotations;
using ClickBusRouterAPI.Api.Contracts;
using ClickBusRouterAPI.Application.Abstractions;
using ClickBusRouterAPI.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClickBusRouterAPI.Api.Controllers;

/// <summary>Cálculo de rotas a pé dentro do terminal.</summary>
[ApiController]
[Route("api/routes")]
[Produces("application/json")]
[Tags("Rotas")]
public sealed class RoutesController(IRouteService routeService) : ControllerBase
{
    /// <summary>Calcula a rota mais rápida a pé até o destino.</summary>
    /// <remarks>
    /// A posição do usuário é encaixada no trecho de corredor mais próximo e a rota é calculada com A*,
    /// usando a distância real entre os pontos. Como a velocidade de caminhada é constante,
    /// a rota mais curta é também a mais rápida.
    ///
    /// Formas de indicar o destino (use só uma):
    /// - `destinationId`: um ponto exato (ex.: `TIETE-TER-SERVICO-031`).
    /// - `destinationQuery`: uma categoria ou nome (ex.: `bilheteria`). A API escolhe o candidato mais próximo pelo caminho.
    ///
    /// Exemplo:
    ///
    ///     POST /api/routes
    ///     {
    ///       "x": 51.81,
    ///       "y": 75.81,
    ///       "destinationQuery": "bilheteria"
    ///     }
    /// </remarks>
    /// <param name="request">Posição do usuário e destino.</param>
    /// <response code="200">Rota encontrada.</response>
    /// <response code="400">Corpo inválido: coordenada ausente ou não numérica, nenhum destino ou os dois destinos informados.</response>
    /// <response code="404">Nenhum ponto com o <c>destinationId</c> informado, ou nenhum nome começando com <c>destinationQuery</c>.</response>
    /// <response code="422">Origem longe demais dos corredores, andar sem mapa ou destino sem caminho até ele (ex.: <c>TIETE-TER-SERVICO-018</c>).</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<RouteResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public ActionResult<RouteResult> Calculate([FromBody] CalculateRouteRequest request) =>
        Ok(routeService.CalculateFastestRoute(request.ToQuery()));

    /// <summary>Calcula a rota mais rápida via query string.</summary>
    /// <remarks>
    /// Mesma operação do <c>POST</c>, útil para testar no navegador. Informe <c>to</c> <b>ou</b> <c>q</c>.
    ///
    ///     GET /api/routes?x=51.81&amp;y=75.81&amp;q=bilheteria
    ///     GET /api/routes?x=26&amp;y=74.32&amp;to=TIETE-TER-SERVICO-015
    /// </remarks>
    /// <param name="x">Coordenada horizontal do usuário (0 = esquerda, 100 = direita da planta).</param>
    /// <param name="y">Coordenada vertical do usuário (0 = topo, 100 = base; cresce para baixo).</param>
    /// <param name="destinationId">Id exato do destino (ex.: <c>TIETE-TER-SERVICO-031</c>). Equivale a <c>destinationId</c> do POST.</param>
    /// <param name="destinationQuery">
    /// Categoria ou início do nome do destino, sem diferenciar acentos (ex.: <c>bilheteria</c>, <c>sanitarios</c>).
    /// Equivale a <c>destinationQuery</c> do POST.
    /// </param>
    /// <param name="floor">Andar do usuário. Opcional; padrão <c>terreo</c> (único andar mapeado hoje).</param>
    /// <response code="200">Rota encontrada.</response>
    /// <response code="400">Parâmetro ausente ou inválido, ou destino informado de nenhuma ou das duas formas.</response>
    /// <response code="404">Destino não encontrado.</response>
    /// <response code="422">Origem fora do mapa ou destino sem caminho.</response>
    [HttpGet]
    [ProducesResponseType<RouteResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public ActionResult<RouteResult> Get(
        [FromQuery, BindRequired, Range(-1000, 1000)] double x,
        [FromQuery, BindRequired, Range(-1000, 1000)] double y,
        [FromQuery(Name = "to"), StringLength(64)] string? destinationId,
        [FromQuery(Name = "q"), StringLength(100)] string? destinationQuery,
        [FromQuery, StringLength(32)] string? floor) =>
        Ok(routeService.CalculateFastestRoute(new RouteQuery(x, y, destinationId, destinationQuery, floor)));
}
