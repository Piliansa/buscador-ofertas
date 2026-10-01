using System.Text.RegularExpressions;

namespace BuscadorOfertas.Filtros;

/// <summary>
/// Decide si una oferta está a tu alcance según el nivel que pide.
/// </summary>
public static class FiltroNivel
{
    /// <summary>Más años de experiencia que esto y la oferta se descarta. Cambialo a tu gusto.</summary>
    public const int MaximoAniosExperiencia = 2;

    // Se buscan solo en el TÍTULO: en la descripción de un puesto mid
    // puede decir "vas a trabajar con devs junior" o "reportás a un lead".
    public static readonly string[] PalabrasSenior =
    [
        "senior", "sr", "lead", "staff", "principal", "head", "director",
        "manager", "architect", "vp", "chief", "expert", "semi senior", "ssr"
    ];

    public static readonly string[] PalabrasJunior =
    [
        "junior", "jr", "entry level", "entry-level", "trainee", "intern",
        "internship", "graduate", "apprentice", "pasante", "pasantia"
    ];

    // Frases de la descripción que indican que no hace falta experiencia.
    public static readonly string[] FrasesSinExperiencia =
    [
        "no experience required", "no prior experience", "no prior professional experience",
        "sin experiencia", "no se requiere experiencia"
    ];

    private static readonly Regex[] _senior = PalabrasSenior.Select(p => FiltroTexto.CrearPatron(p)).ToArray();
    private static readonly Regex[] _junior = PalabrasJunior.Select(p => FiltroTexto.CrearPatron(p)).ToArray();
    private static readonly Regex[] _sinExperiencia = FrasesSinExperiencia.Select(p => FiltroTexto.CrearPatron(p)).ToArray();

    // Formas comunes de pedir años de experiencia:
    //   "5+ years", "3-5 years", "at least 4 years", "minimum 3 years",
    //   "4 years of experience", "3 años de experiencia"
    // NoEsFinDeRango evita leer el "5" de "3-5 years of experience" como requisito:
    // en un rango cuenta el mínimo (3), que ya lo captura el segundo patrón.
    private const string NoEsFinDeRango = @"(?<!\d\s*(?:-|–|to|a)\s*)";

    private static readonly Regex[] _anios =
    [
        new(NoEsFinDeRango + @"(\d{1,2})\s*\+\s*(?:years?|yrs?|anos)", RegexOptions.Compiled),
        new(@"(\d{1,2})\s*(?:-|–|to|a)\s*\d{1,2}\s*(?:years?|yrs?|anos)", RegexOptions.Compiled),
        new(@"(?:at least|minimum(?: of)?|min\.?|al menos|minimo(?: de)?)\s*(\d{1,2})\s*(?:years?|yrs?|anos)", RegexOptions.Compiled),
        new(NoEsFinDeRango + @"(\d{1,2})\s*(?:years?|yrs?)\s+of\s+(?:\S+\s+){0,3}?experience", RegexOptions.Compiled),
        new(NoEsFinDeRango + @"(\d{1,2})\s*anos\s+de\s+experiencia", RegexOptions.Compiled),
    ];

    /// <summary>¿El título suena a puesto senior?</summary>
    public static bool EsSenior(string titulo) =>
        FiltroTexto.ContieneAlguna(FiltroTexto.Normalizar(titulo), _senior);

    /// <summary>¿Dice explícitamente que es junior o que no hace falta experiencia?</summary>
    public static bool EsJunior(string titulo, string textoNormalizado) =>
        FiltroTexto.ContieneAlguna(FiltroTexto.Normalizar(titulo), _junior)
        || FiltroTexto.ContieneAlguna(textoNormalizado, _sinExperiencia);

    /// <summary>
    /// Los años de experiencia que pide la oferta (el mayor que encuentre),
    /// o null si no menciona ninguno.
    /// </summary>
    public static int? AniosPedidos(string textoNormalizado)
    {
        var encontrados = _anios
            .SelectMany(r => r.Matches(textoNormalizado))
            .Select(m => int.Parse(m.Groups[1].Value))
            .Where(n => n <= 30) // descarta cosas como "2015-2020 years"
            .ToList();

        return encontrados.Count == 0 ? null : encontrados.Max();
    }

    /// <summary>¿El nivel que piden está a tu alcance?</summary>
    public static bool EstaAMiAlcance(string titulo, string textoNormalizado) =>
        !EsSenior(titulo)
        && (AniosPedidos(textoNormalizado) ?? 0) <= MaximoAniosExperiencia;
}
