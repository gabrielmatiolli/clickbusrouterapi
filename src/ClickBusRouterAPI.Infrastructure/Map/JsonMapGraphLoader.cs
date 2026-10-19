using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ClickBusRouterAPI.Domain.Abstractions;
using ClickBusRouterAPI.Domain.Entities;
using ClickBusRouterAPI.Domain.ValueObjects;

namespace ClickBusRouterAPI.Infrastructure.Map;

public sealed class JsonMapGraphLoader(IOptions<MapDataOptions> options, ILogger<JsonMapGraphLoader> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public IMapGraph Load()
    {
        var path = Path.IsPathRooted(options.Value.FilePath)
            ? options.Value.FilePath
            : Path.Combine(AppContext.BaseDirectory, options.Value.FilePath);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo do mapa não encontrado: {path}", path);

        using var stream = File.OpenRead(path);
        var graph = Parse(stream, logger);

        logger.LogInformation("Mapa carregado de {Path}: {Points} pontos, {Segments} conexões.",
            path, graph.Points.Count, graph.Segments.Length);

        return graph;
    }

    /// <summary>Lê o JSON, valida a consistência e congela o grafo. Público para facilitar testes.</summary>
    public static IMapGraph Parse(Stream json, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        var document = JsonSerializer.Deserialize<MapDocumentJson>(json, SerializerOptions)
            ?? throw new InvalidDataException("JSON do mapa vazio ou inválido.");

        var points = new Dictionary<string, MapPoint>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in document.Points)
        {
            if (string.IsNullOrWhiteSpace(p.Id))
                throw new InvalidDataException("Ponto sem 'id' no mapa.");

            var type = Enum.TryParse<PointType>(p.Type, ignoreCase: true, out var parsed) ? parsed : PointType.Desconhecido;
            var point = new MapPoint(p.Id.Trim(), p.Label.Trim(), p.Floor.Trim(), type, new Coordinate(p.X, p.Y));

            if (!points.TryAdd(point.Id, point))
                throw new InvalidDataException($"Id duplicado no mapa: {point.Id}");
        }

        var adjacency = points.Keys.ToDictionary(id => id, _ => new List<Edge>(), StringComparer.OrdinalIgnoreCase);
        var segments = ImmutableArray.CreateBuilder<Segment>(document.Connections.Count);
        var seen = new HashSet<(string, string)>();

        foreach (var c in document.Connections)
        {
            if (!points.TryGetValue(c.From, out var a) || !points.TryGetValue(c.To, out var b))
                throw new InvalidDataException($"Conexão aponta para ponto inexistente: {c.From} -> {c.To}");

            if (a.Id == b.Id)
            {
                logger.LogWarning("Conexão de um ponto para ele mesmo ignorada: {Id}", a.Id);
                continue;
            }

            // Conexões são bidirecionais: normaliza a chave para não duplicar A->B e B->A.
            var key = string.CompareOrdinal(a.Id, b.Id) < 0 ? (a.Id, b.Id) : (b.Id, a.Id);
            if (!seen.Add(key))
                continue;

            var length = a.Position.DistanceTo(b.Position);
            adjacency[a.Id].Add(new Edge(a.Id, b.Id, length));
            adjacency[b.Id].Add(new Edge(b.Id, a.Id, length));
            segments.Add(new Segment(a, b, length));
        }

        foreach (var (id, edges) in adjacency)
        {
            if (edges.Count == 0)
                logger.LogWarning("Ponto {Id} ({Label}) não tem conexões e ficará inacessível.", id, points[id].Label);
        }

        return new InMemoryMapGraph(
            points.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            adjacency.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToImmutableArray(), StringComparer.OrdinalIgnoreCase),
            segments.ToImmutable());
    }
}
