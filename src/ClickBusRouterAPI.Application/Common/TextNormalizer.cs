using System.Globalization;
using System.Text;

namespace ClickBusRouterAPI.Application.Common;

/// <summary>Normaliza rótulos para busca: minúsculas, sem acento, espaços colapsados.</summary>
public static class TextNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(ch))
            {
                if (!previousWasSpace)
                    builder.Append(' ');
                previousWasSpace = true;
                continue;
            }

            builder.Append(ch);
            previousWasSpace = false;
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
