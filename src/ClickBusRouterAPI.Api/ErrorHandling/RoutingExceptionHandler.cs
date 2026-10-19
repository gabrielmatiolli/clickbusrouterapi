using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ClickBusRouterAPI.Application.Exceptions;

namespace ClickBusRouterAPI.Api.ErrorHandling;

/// <summary>Traduz exceções de negócio em ProblemDetails (RFC 7807) com o status HTTP adequado.</summary>
public sealed class RoutingExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not RoutingException routing)
            return false;

        var (status, title) = routing switch
        {
            InvalidRouteRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            DestinationNotFoundException => (StatusCodes.Status404NotFound, "Destino não encontrado"),
            OriginOutOfMapException => (StatusCodes.Status422UnprocessableEntity, "Origem fora do mapa"),
            RouteNotFoundException => (StatusCodes.Status422UnprocessableEntity, "Rota inexistente"),
            _ => (StatusCodes.Status400BadRequest, "Erro de roteamento")
        };

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = routing.Message }
        });
    }
}
