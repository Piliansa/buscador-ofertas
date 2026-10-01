using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BuscadorOfertas.Filtros;

/// <summary>Herramientas de texto que usan todos los filtros.</summary>
public static class FiltroTexto
{
    private static readonly Regex _etiquetasHtml = new("<[^>]+>", RegexOptions.Compiled);

    /// <summary>
    /// Deja el texto listo para comparar: saca etiquetas HTML ("&lt;p&gt;"),
    /// decodifica entidades ("&amp;nbsp;"), pasa a minúsculas y saca tildes.
    /// Así "Ambiental", "ambiental" y "&lt;b&gt;AMBIENTAL&lt;/b&gt;" coinciden.
    /// </summary>
    public static string Normalizar(string texto)
    {
        var sinHtml = WebUtility.HtmlDecode(_etiquetasHtml.Replace(texto, " "));
        var descompuesto = sinHtml.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Busca la palabra completa: "carbon" no debe coincidir dentro de "carbonara".
    /// Usamos "no hay letra ni número antes/después" en lugar de \b para que
    /// funcione también con palabras como "c#" o ".net".
    /// </summary>
    public static Regex CrearPatron(string palabra) =>
        new($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(Normalizar(palabra))}(?![\p{{L}}\p{{N}}])",
            RegexOptions.Compiled);

    public static bool ContieneAlguna(string textoNormalizado, IEnumerable<Regex> patrones) =>
        patrones.Any(p => p.IsMatch(textoNormalizado));
}
