namespace ClickBusRouterAPI.Domain.ValueObjects;

/// <summary>
/// Posição no plano do mapa. As unidades são as mesmas do JSON (percentual da planta),
/// com o eixo Y crescendo para baixo (coordenadas de tela).
/// </summary>
public readonly record struct Coordinate(double X, double Y)
{
    public double DistanceTo(Coordinate other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static Coordinate operator -(Coordinate a, Coordinate b) => new(a.X - b.X, a.Y - b.Y);
}
