namespace BuscadorOfertas.Models;

/// <summary>
/// Una oferta laboral, venga de la fuente que venga.
/// Es un "record": un tipo inmutable pensado para guardar datos.
/// Dos records con los mismos valores se consideran iguales.
/// </summary>
public record Oferta(
    string Id,
    string Titulo,
    string Organizacion,
    string Ubicacion,
    string Url,
    string Fuente,
    DateTime? Publicada,
    string TextoCompleto, // título + descripción + etiquetas, para buscar palabras clave
    // Desde dónde se aceptan postulantes (ej: "Worldwide", "USA", "UTC-5 to UTC+1").
    // null = la fuente no lo informa, y en ese caso no filtramos por ubicación.
    string? RegionPostulacion = null
);
