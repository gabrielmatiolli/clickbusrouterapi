using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Application.Routing;

/// <summary>
/// Converte a mudança de direção num ponto em uma instrução legível.
/// O eixo Y do mapa cresce para baixo, então produto vetorial positivo = curva à direita.
/// </summary>
internal static class TurnDescriber
{
    private const double Epsilon = 1e-9;

    public static string? Describe(Coordinate? previous, Coordinate pivot, Coordinate next)
    {
        if (previous is null)
            return null;

        var incoming = pivot - previous.Value;
        var outgoing = next - pivot;

        if (IsZero(incoming) || IsZero(outgoing))
            return null;

        var cross = incoming.X * outgoing.Y - incoming.Y * outgoing.X;
        var dot = incoming.X * outgoing.X + incoming.Y * outgoing.Y;
        var angle = Math.Atan2(cross, dot) * 180 / Math.PI;
        var magnitude = Math.Abs(angle);

        return magnitude switch
        {
            < 25.0 => "Siga em frente",
            > 155.0 => "Retorne",
            < 60.0 => angle > 0 ? "Mantenha-se à direita" : "Mantenha-se à esquerda",
            _ => angle > 0 ? "Vire à direita" : "Vire à esquerda"
        };
    }

    private static bool IsZero(Coordinate v) => Math.Abs(v.X) < Epsilon && Math.Abs(v.Y) < Epsilon;
}
