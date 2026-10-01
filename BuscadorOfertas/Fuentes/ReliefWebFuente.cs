using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Fuentes;

/// <summary>
/// Empleos de ONGs y organismos humanitarios desde la API de ReliefWeb (ONU / OCHA).
/// Requiere un "appname" preaprobado: se pide gratis con el formulario
/// enlazado en https://apidoc.reliefweb.int/parameters (no hace falta crear cuenta).
/// </summary>
public class ReliefWebFuente : IFuenteOfertas
{
    private readonly HttpClient _http;
    private readonly string _appName;

    // Búsqueda amplia: después FiltroIntereses decide qué es relevante de verdad.
    private const string Consulta =
        "environment OR environmental OR climate OR conservation OR biodiversity OR " +
        "nature OR sustainability OR archive OR archival OR heritage OR historical OR " +
        "developer OR software OR programmer";

    public ReliefWebFuente(HttpClient http, string appName)
    {
        _http = http;
        _appName = appName;
    }

    public string Nombre => "ReliefWeb";

    public async Task<IReadOnlyList<Oferta>> ObtenerOfertasAsync(CancellationToken ct = default)
    {
        var url = $"https://api.reliefweb.int/v2/jobs?appname={Uri.EscapeDataString(_appName)}";

        // La API acepta la búsqueda como JSON en el cuerpo de un POST.
        var cuerpo = new
        {
            limit = 100,
            preset = "latest",
            query = new { value = Consulta },
            fields = new
            {
                include = new[] { "title", "url", "body", "source.name", "country.name", "date.created" }
            }
        };

        var respuesta = await _http.PostAsJsonAsync(url, cuerpo, ct);
        respuesta.EnsureSuccessStatusCode(); // si la API devuelve error, lanza una excepción

        var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaReliefWeb>(ct);
        return (datos?.Data ?? []).Select(Convertir).ToList();
    }

    internal static Oferta Convertir(ItemReliefWeb item)
    {
        var f = item.Fields;
        var organizacion = string.Join(", ", f?.Source?.Select(s => s.Name) ?? []);
        var pais = string.Join(", ", f?.Country?.Select(c => c.Name) ?? []);

        return new Oferta(
            Id: $"reliefweb-{item.Id}",
            Titulo: f?.Title ?? "(sin título)",
            Organizacion: organizacion,
            Ubicacion: pais,
            Url: f?.Url ?? "",
            Fuente: "ReliefWeb",
            Publicada: f?.Date?.Created,
            // El país de ReliefWeb es donde se trabaja, no desde dónde se puede
            // postular, así que no lo usamos como RegionPostulacion.
            TextoCompleto: $"{f?.Title} {f?.Body}");
    }

    internal record RespuestaReliefWeb([property: JsonPropertyName("data")] List<ItemReliefWeb>? Data);

    // El id puede venir como texto o como número: JsonElement acepta ambos.
    internal record ItemReliefWeb(
        [property: JsonPropertyName("id")] JsonElement Id,
        [property: JsonPropertyName("fields")] CamposReliefWeb? Fields);

    internal record CamposReliefWeb(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("source")] List<ConNombre>? Source,
        [property: JsonPropertyName("country")] List<ConNombre>? Country,
        [property: JsonPropertyName("date")] FechasReliefWeb? Date);

    internal record ConNombre([property: JsonPropertyName("name")] string? Name);

    internal record FechasReliefWeb([property: JsonPropertyName("created")] DateTime? Created);
}
