using System.ComponentModel;

namespace ClickBusRouterAPI.Domain.Entities;

/// <summary>
/// Categoria de um ponto do mapa. Na API é serializado como texto em minúsculas
/// (<c>entrada</c>, <c>corredor</c>, <c>servico</c>).
/// </summary>
public enum PointType
{
    /// <summary>Tipo não reconhecido no JSON do mapa. Não aparece no mapa atual.</summary>
    [Description("Tipo não reconhecido no arquivo do mapa (não ocorre no mapa atual).")]
    Desconhecido = 0,

    /// <summary>Acesso ao terminal: metrô, Cruzeiro do Sul, estacionamento.</summary>
    [Description("Acesso ao terminal (ex.: Acesso metro, Cruzeiro do sul, estacionamento).")]
    Entrada = 1,

    /// <summary>Nó de passagem usado para montar os caminhos. Normalmente não é destino final.</summary>
    [Description("Nó de passagem entre pontos; usado para montar o caminho (ex.: Corredor 82).")]
    Corredor = 2,

    /// <summary>Serviço ou comércio: bilheterias, sanitários, alimentação, lojas, etc.</summary>
    [Description("Serviço ou comércio (ex.: bilheteria, sanitários, alimentação, lojas, farmácia, táxi).")]
    Servico = 3
}
