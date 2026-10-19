using ClickBusRouterAPI.Application.Common;

namespace ClickBusRouterAPI.UnitTests.Application;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("Sanitários", "sanitarios")]
    [InlineData("  Praça   de  Alimentação ", "praca de alimentacao")]
    [InlineData("FARMÁCIA", "farmacia")]
    [InlineData("informação 2", "informacao 2")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normaliza_caixa_acentos_e_espacos(string? input, string expected) =>
        Assert.Equal(expected, TextNormalizer.Normalize(input));
}
