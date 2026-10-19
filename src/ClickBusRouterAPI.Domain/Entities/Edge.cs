namespace ClickBusRouterAPI.Domain.Entities;

/// <summary>Aresta direcionada da lista de adjacência (cada conexão do JSON gera duas, uma por sentido).</summary>
public readonly record struct Edge(string FromId, string ToId, double Length);

/// <summary>Trecho físico entre dois pontos. Usado para "encaixar" a posição do usuário na malha.</summary>
public sealed record Segment(MapPoint A, MapPoint B, double Length);
