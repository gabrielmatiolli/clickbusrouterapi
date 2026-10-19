using System.ComponentModel.DataAnnotations;
using ClickBusRouterAPI.Application.Abstractions;
using ClickBusRouterAPI.Application.Contracts;
using ClickBusRouterAPI.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ClickBusRouterAPI.Api.Controllers;

/// <summary>Consulta dos pontos do mapa (entradas, corredores e serviços).</summary>
[ApiController]
[Route("api/points")]
[Produces("application/json")]
[Tags("Pontos")]
public sealed class PointsController(IPointService pointService) : ControllerBase
{
    /// <summary>Lista os pontos do mapa.</summary>
    /// <remarks>
    /// Use para descobrir os ids e nomes aceitos em <c>destinationId</c> e <c>destinationQuery</c>.
    /// Os filtros são combinados (E). Resultado ordenado por tipo e depois por nome.
    ///
    ///     GET /api/points?type=servico&amp;search=bilheteria
    /// </remarks>
    /// <param name="type">
    /// Categoria do ponto. Valores aceitos:
    /// <c>entrada</c> (acessos ao terminal),
    /// <c>corredor</c> (nós de passagem) e
    /// <c>servico</c> (bilheterias, sanitários, alimentação, lojas…).
    /// Omitido = todos. Valor fora da lista retorna <c>400</c>.
    /// </param>
    /// <param name="search">Trecho do nome, em qualquer posição, sem diferenciar acentos ou maiúsculas (ex.: <c>sanit</c>).</param>
    /// <response code="200">Lista de pontos (pode ser vazia).</response>
    /// <response code="400">Valor de <c>type</c> inválido.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PointDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public ActionResult<IReadOnlyList<PointDto>> List(
        [FromQuery] PointType? type,
        [FromQuery, StringLength(100)] string? search) =>
        Ok(pointService.List(type, search));

    /// <summary>Busca um ponto pelo id.</summary>
    /// <param name="id">Id do ponto, sem diferenciar maiúsculas (ex.: <c>TIETE-TER-SERVICO-008</c>).</param>
    /// <response code="200">Ponto encontrado.</response>
    /// <response code="404">Nenhum ponto com esse id.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<PointDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<PointDto> GetById(string id) =>
        pointService.GetById(id) is { } point ? Ok(point) : NotFound();
}
