namespace ClickBusRouterAPI.Application.Exceptions;

public abstract class RoutingException(string message) : Exception(message);

/// <summary>Requisição inconsistente (ex.: destino ausente ou informado de duas formas).</summary>
public sealed class InvalidRouteRequestException(string message) : RoutingException(message);

/// <summary>Nenhum ponto corresponde ao destino informado.</summary>
public sealed class DestinationNotFoundException(string message) : RoutingException(message);

/// <summary>A posição do usuário está longe demais de qualquer corredor mapeado.</summary>
public sealed class OriginOutOfMapException(string message) : RoutingException(message);

/// <summary>Existe destino, mas não há caminho conectado até ele.</summary>
public sealed class RouteNotFoundException(string message) : RoutingException(message);
