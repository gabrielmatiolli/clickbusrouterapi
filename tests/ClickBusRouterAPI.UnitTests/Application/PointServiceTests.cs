using ClickBusRouterAPI.Application.Services;
using ClickBusRouterAPI.Domain.Entities;

namespace ClickBusRouterAPI.UnitTests.Application;

public sealed class PointServiceTests
{
    private readonly PointService _sut = new(TestMaps.Tiete);

    [Fact]
    public void Lista_todos_sem_filtro() =>
        Assert.Equal(69, _sut.List().Count);

    [Theory]
    [InlineData(PointType.Entrada, 3)]
    [InlineData(PointType.Servico, 32)]
    [InlineData(PointType.Corredor, 34)]
    [InlineData(PointType.Desconhecido, 0)]
    public void Filtra_por_tipo(PointType type, int expected)
    {
        var points = _sut.List(type);

        Assert.Equal(expected, points.Count);
        Assert.All(points, p => Assert.Equal(type, p.Type));
    }

    [Fact]
    public void Ordena_por_tipo_e_depois_por_nome()
    {
        var types = _sut.List().Select(p => p.Type).ToList();

        Assert.Equal(types.OrderBy(t => t), types);
        Assert.Equal(PointType.Entrada, types[0]);
    }

    [Fact]
    public void Busca_por_nome_ignora_acento()
    {
        var points = _sut.List(search: "sanitarios");

        Assert.Equal(new[] { "sanitários", "sanitários 2" }, points.Select(p => p.Label));
    }

    [Fact]
    public void Busca_por_bilheteria_encontra_as_dez()
    {
        var points = _sut.List(PointType.Servico, "bilheteria");

        Assert.Equal(10, points.Count);
    }

    [Fact]
    public void Busca_por_id()
    {
        var point = _sut.GetById("TIETE-TER-SERVICO-031");

        Assert.NotNull(point);
        Assert.Equal("praça de alimentação", point.Label);
        Assert.Equal(PointType.Servico, point.Type);
    }

    [Fact]
    public void Id_inexistente_retorna_null() =>
        Assert.Null(_sut.GetById("NAO-EXISTE"));
}
