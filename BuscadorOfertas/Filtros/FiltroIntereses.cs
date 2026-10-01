using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Filtros;

/// <summary>Una oferta junto con el motivo por el que nos interesa.</summary>
public record OfertaEvaluada(Oferta Oferta, bool EsDesarrollo, IReadOnlyList<string> TemasEncontrados);

/// <summary>
/// Decide qué ofertas son interesantes:
///  - tienen que tocar algún tema (ambiente/naturaleza o documentación histórica), y
///  - las de desarrollo de software van primero.
/// </summary>
public class FiltroIntereses
{
    // Ojo con las palabras demasiado comunes: "environment" aparece en
    // "fast-paced environment" y "nature" en "the nature of the role".
    // Por eso usamos palabras y frases más específicas.
    public static readonly string[] PalabrasAmbiente =
    [
        "environmental", "climate", "conservation", "biodiversity", "sustainability",
        "sustainable", "ecology", "ecological", "wildlife", "forest", "forestry", "ocean",
        "renewable", "clean energy", "carbon", "emissions", "impact assessment",
        "nature conservation", "nature-based",
        "medio ambiente", "ambiental", "naturaleza", "biodiversidad", "sostenibilidad",
        "cambio climatico", "impacto ambiental"
    ];

    public static readonly string[] PalabrasHistoria =
    [
        "archive", "archives", "archival", "archivist", "heritage", "historical",
        "museum", "librarian", "digitization", "digitisation", "digital humanities",
        "archivo", "archivistica", "patrimonio", "historico", "historica", "museo"
    ];

    public static readonly string[] PalabrasDesarrollo =
    [
        "developer", "software", "programmer", "full stack", "full-stack", "fullstack",
        "frontend", "front-end", "front end", "backend", "back-end", "web development",
        "c#", ".net", "asp.net", "javascript", "typescript",
        "desarrollador", "desarrolladora", "programador", "programadora", "desarrollo web"
    ];

    private readonly List<(string Palabra, Regex Patron)> _temas;
    private readonly List<Regex> _desarrollo;

    public FiltroIntereses()
    {
        _temas = PalabrasAmbiente.Concat(PalabrasHistoria)
            .Select(p => (p, CrearPatron(p)))
            .ToList();
        _desarrollo = PalabrasDesarrollo.Select(CrearPatron).ToList();
    }

    /// <summary>Evalúa una oferta. Devuelve null si no toca ninguno de los temas.</summary>
    public OfertaEvaluada? Evaluar(Oferta oferta)
    {
        var texto = Normalizar(oferta.TextoCompleto);

        var temas = _temas
            .Where(t => t.Patron.IsMatch(texto))
            .Select(t => t.Palabra)
            .ToList();

        if (temas.Count == 0)
            return null;

        var esDesarrollo = _desarrollo.Any(p => p.IsMatch(texto));
        return new OfertaEvaluada(oferta, esDesarrollo, temas);
    }

    /// <summary>
    /// Filtra y ordena: primero desarrollo, después las que tocan más temas,
    /// y a igualdad, las más nuevas. Si hay palabras extra, deben aparecer todas.
    /// </summary>
    public IReadOnlyList<OfertaEvaluada> FiltrarYOrdenar(
        IEnumerable<Oferta> ofertas, string? palabrasExtra = null)
    {
        var extras = (palabrasExtra ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CrearPatron)
            .ToList();

        return ofertas
            .DistinctBy(o => string.IsNullOrEmpty(o.Url) ? o.Id : o.Url)
            .Where(o => extras.All(e => e.IsMatch(Normalizar(o.TextoCompleto))))
            .Select(Evaluar)
            .OfType<OfertaEvaluada>() // descarta los null
            .OrderByDescending(e => e.EsDesarrollo)
            .ThenByDescending(e => e.TemasEncontrados.Count)
            .ThenByDescending(e => e.Oferta.Publicada ?? DateTime.MinValue)
            .ToList();
    }

    /// <summary>Pasa a minúsculas y saca tildes: "Ambiental" y "ambiental" coinciden, "histórico" y "historico" también.</summary>
    public static string Normalizar(string texto)
    {
        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // Busca la palabra completa: "carbon" no debe coincidir dentro de "carbonara".
    // Usamos "no hay letra ni número antes/después" en lugar de \b para que
    // funcione también con palabras como "c#" o ".net".
    private static Regex CrearPatron(string palabra) =>
        new($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(Normalizar(palabra))}(?![\p{{L}}\p{{N}}])",
            RegexOptions.Compiled);
}
