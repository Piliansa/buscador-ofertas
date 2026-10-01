using System.Text.RegularExpressions;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Filtros;

/// <summary>
/// Por qué una oferta te interesa. El número es la prioridad:
/// cuanto más alto, más arriba aparece en los resultados.
/// </summary>
public enum TipoOferta
{
    Flexible = 1,          // 🧩 revisión de contenido, evaluación de IA, etiquetado de datos…
    Tema = 2,              // 🌿 ambiente o historia, aunque no sea de desarrollo
    Desarrollo = 3,        // 💻 desarrollo sin seniority alta
    DesarrolloJunior = 4,  // 💻 desarrollo que dice explícitamente "junior"
    DesarrolloConTema = 5  // 💻🌿 lo ideal: desarrollo en un tema que te importa
}

/// <summary>Una oferta junto con el motivo por el que nos interesa.</summary>
public record OfertaEvaluada(Oferta Oferta, TipoOferta Tipo, IReadOnlyList<string> TemasEncontrados)
{
    public bool EsDesarrollo => Tipo >= TipoOferta.Desarrollo;
}

/// <summary>
/// Decide qué ofertas te sirven. Una oferta pasa si:
///  1. te podés postular desde donde estás (FiltroUbicacion),
///  2. el nivel está a tu alcance (FiltroNivel), y
///  3. encaja en alguna categoría de TipoOferta.
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

    // Trabajos remotos por tarea, sin experiencia previa, que se aprenden rápido.
    public static readonly string[] PalabrasFlexibles =
    [
        "content reviewer", "content review", "search evaluator", "search quality",
        "rater", "ai trainer", "ai training", "data annotation", "data annotator",
        "data labeling", "data labelling", "transcription", "transcriber",
        "content moderator", "content moderation", "evaluador", "anotacion de datos"
    ];

    private readonly List<(string Palabra, Regex Patron)> _temas;
    private readonly Regex[] _desarrollo;
    private readonly Regex[] _flexibles;

    public FiltroIntereses()
    {
        _temas = PalabrasAmbiente.Concat(PalabrasHistoria)
            .Select(p => (p, FiltroTexto.CrearPatron(p)))
            .ToList();
        _desarrollo = PalabrasDesarrollo.Select(p => FiltroTexto.CrearPatron(p)).ToArray();
        _flexibles = PalabrasFlexibles.Select(p => FiltroTexto.CrearPatron(p)).ToArray();
    }

    /// <summary>Evalúa una oferta. Devuelve null si no te sirve.</summary>
    public OfertaEvaluada? Evaluar(Oferta oferta)
    {
        if (!FiltroUbicacion.PuedoPostularme(oferta.RegionPostulacion))
            return null;

        var texto = FiltroTexto.Normalizar(oferta.TextoCompleto);

        if (!FiltroNivel.EstaAMiAlcance(oferta.Titulo, texto))
            return null;

        var temas = _temas
            .Where(t => t.Patron.IsMatch(texto))
            .Select(t => t.Palabra)
            .ToList();

        // Para "desarrollo" y "flexible" miramos el título: la descripción de
        // cualquier empresa puede nombrar "software" sin que el puesto lo sea.
        var titulo = FiltroTexto.Normalizar(oferta.Titulo);
        var esDesarrollo = FiltroTexto.ContieneAlguna(titulo, _desarrollo);
        var esFlexible = FiltroTexto.ContieneAlguna(titulo, _flexibles);

        TipoOferta? tipo = (esDesarrollo, temas.Count > 0, esFlexible) switch
        {
            (true, true, _) => TipoOferta.DesarrolloConTema,
            (true, false, _) when FiltroNivel.EsJunior(oferta.Titulo, texto) => TipoOferta.DesarrolloJunior,
            (true, false, _) => TipoOferta.Desarrollo,
            (false, true, _) => TipoOferta.Tema,
            (false, false, true) => TipoOferta.Flexible,
            _ => null
        };

        return tipo is null ? null : new OfertaEvaluada(oferta, tipo.Value, temas);
    }

    /// <summary>
    /// Filtra y ordena: primero por tipo (ver TipoOferta), después las que tocan
    /// más temas, y a igualdad, las más nuevas. Si hay palabras extra, deben aparecer todas.
    /// </summary>
    public IReadOnlyList<OfertaEvaluada> FiltrarYOrdenar(
        IEnumerable<Oferta> ofertas, string? palabrasExtra = null)
    {
        var extras = (palabrasExtra ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => FiltroTexto.CrearPatron(p))
            .ToList();

        return ofertas
            .DistinctBy(o => string.IsNullOrEmpty(o.Url) ? o.Id : o.Url)
            .Where(o => extras.All(e => e.IsMatch(FiltroTexto.Normalizar(o.TextoCompleto))))
            .Select(Evaluar)
            .OfType<OfertaEvaluada>() // descarta los null
            .OrderByDescending(e => e.Tipo)
            .ThenByDescending(e => e.TemasEncontrados.Count)
            .ThenByDescending(e => e.Oferta.Publicada ?? DateTime.MinValue)
            .ToList();
    }
}
