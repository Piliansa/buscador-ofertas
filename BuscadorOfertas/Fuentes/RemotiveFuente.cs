using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Fuentes;

/// <summary>
/// Trabajos remotos desde la API pública de Remotive.
/// Remotive pide que se enlace a la oferta original y se mencione la fuente.
/// </summary>
public class RemotiveFuente : IFuenteOfertas
{
    private const string UrlApi = "https://remotive.com/api/remote-jobs";
    private static readonly TimeSpan DuracionCache = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private IReadOnlyList<Oferta>? _cache;
    private DateTime _cacheHasta = DateTime.MinValue;

    public RemotiveFuente(HttpClient http) => _http = http;

    public string Nombre => "Remotive";

    public async Task<IReadOnlyList<Oferta>> ObtenerOfertasAsync(CancellationToken ct = default)
    {
        // El feed es grande: si lo pedimos hace menos de una hora, reusamos el resultado.
        if (_cache is not null && DateTime.UtcNow < _cacheHasta)
            return _cache;

        var respuesta = await _http.GetFromJsonAsync<RespuestaRemotive>(UrlApi, ct);

        _cache = (respuesta?.Jobs ?? [])
            .Select(Convertir)
            .ToList();
        _cacheHasta = DateTime.UtcNow + DuracionCache;
        return _cache;
    }

    // Pasa del formato de Remotive a nuestro modelo Oferta.
    internal static Oferta Convertir(TrabajoRemotive t) => new(
        Id: $"remotive-{t.Id}",
        Titulo: t.Title ?? "(sin título)",
        Organizacion: t.CompanyName ?? "",
        Ubicacion: string.IsNullOrWhiteSpace(t.Location) ? "Remoto" : $"Remoto ({t.Location})",
        Url: t.Url ?? "",
        Fuente: "Remotive",
        Publicada: DateTime.TryParse(t.PublicationDate, out var fecha) ? fecha : null,
        TextoCompleto: string.Join(" ",
            t.Title, t.Category, string.Join(" ", t.Tags ?? []), t.Description)
    );

    // Clases que reflejan el JSON de Remotive. [JsonPropertyName] conecta
    // el nombre en el JSON (snake_case) con la propiedad en C# (PascalCase).
    internal record RespuestaRemotive(
        [property: JsonPropertyName("jobs")] List<TrabajoRemotive>? Jobs);

    internal record TrabajoRemotive(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("company_name")] string? CompanyName,
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("tags")] List<string>? Tags,
        [property: JsonPropertyName("publication_date")] string? PublicationDate,
        [property: JsonPropertyName("candidate_required_location")] string? Location,
        [property: JsonPropertyName("description")] string? Description);
}
