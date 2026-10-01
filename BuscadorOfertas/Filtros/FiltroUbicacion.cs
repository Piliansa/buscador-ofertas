using System.Text.RegularExpressions;

namespace BuscadorOfertas.Filtros;

/// <summary>
/// Decide si te podés postular según la región que pide la oferta.
/// Ejemplos: "Worldwide" ✔, "LATAM" ✔, "USA" ✘, "UTC-5 to UTC+1" ✔ (incluye UTC-3).
/// </summary>
public static class FiltroUbicacion
{
    // Tu huso horario: Argentina es UTC-3.
    public const int MiUtc = -3;

    public static readonly string[] RegionesPermitidas =
    [
        "worldwide", "anywhere", "global", "anywhere in the world",
        "latam", "latin america", "south america", "americas", "argentina",
        "todo el mundo", "latinoamerica", "america latina"
    ];

    private static readonly Regex[] _patrones = RegionesPermitidas
        .Select(r => FiltroTexto.CrearPatron(r))
        .ToArray();

    // Captura rangos como "UTC-5 to UTC+1", "GMT-3 - GMT+2" o "UTC−4/UTC+3".
    private static readonly Regex _rangoHorario = new(
        @"(?:utc|gmt)\s*([+\-−]\s*\d{1,2})(?::\d{2})?\s*(?:to|a|-|–|/|and|y)\s*(?:utc|gmt)\s*([+\-−]\s*\d{1,2})",
        RegexOptions.Compiled);

    public static bool PuedoPostularme(string? region)
    {
        // Si la fuente no dice nada, no la descartamos.
        if (string.IsNullOrWhiteSpace(region))
            return true;

        var texto = FiltroTexto.Normalizar(region);

        if (_patrones.Any(p => p.IsMatch(texto)))
            return true;

        foreach (Match m in _rangoHorario.Matches(texto))
        {
            var desde = LeerUtc(m.Groups[1].Value);
            var hasta = LeerUtc(m.Groups[2].Value);
            if (Math.Min(desde, hasta) <= MiUtc && MiUtc <= Math.Max(desde, hasta))
                return true;
        }

        return false;
    }

    // "+ 3" → 3, "-5" → -5, "−4" (signo menos tipográfico) → -4
    private static int LeerUtc(string valor) =>
        int.Parse(valor.Replace("−", "-").Replace(" ", ""));
}
