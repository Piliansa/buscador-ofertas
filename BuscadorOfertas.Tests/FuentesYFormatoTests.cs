using System.Text.Json;
using BuscadorOfertas.Bot;
using BuscadorOfertas.Filtros;
using BuscadorOfertas.Fuentes;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Tests;

public class FuentesYFormatoTests
{
    [Fact]
    public void Remotive_convierte_el_json_a_Oferta()
    {
        // Un ejemplo chico con la forma del JSON de Remotive.
        const string json = """
        {
          "jobs": [{
            "id": 123,
            "url": "https://remotive.com/remote-jobs/software-dev/x-123",
            "title": "Full Stack Developer",
            "company_name": "Green Org",
            "category": "Software Development",
            "tags": ["react", "climate"],
            "publication_date": "2026-09-30T10:00:00",
            "candidate_required_location": "LATAM",
            "description": "<p>Help us fight climate change</p>"
          }]
        }
        """;

        var respuesta = JsonSerializer.Deserialize<RemotiveFuente.RespuestaRemotive>(json);
        var oferta = RemotiveFuente.Convertir(respuesta!.Jobs![0]);

        Assert.Equal("remotive-123", oferta.Id);
        Assert.Equal("Green Org", oferta.Organizacion);
        Assert.Equal("Remoto (LATAM)", oferta.Ubicacion);
        Assert.Equal("LATAM", oferta.RegionPostulacion);
        Assert.Contains("climate", oferta.TextoCompleto);
    }

    [Fact]
    public void ReliefWeb_convierte_el_json_a_Oferta()
    {
        const string json = """
        {
          "data": [{
            "id": "456",
            "fields": {
              "title": "Environmental Data Developer",
              "url": "https://reliefweb.int/job/456",
              "body": "Build tools for biodiversity monitoring",
              "source": [{ "name": "UNEP" }],
              "country": [{ "name": "Kenya" }],
              "date": { "created": "2026-09-28T00:00:00+00:00" }
            }
          }]
        }
        """;

        var respuesta = JsonSerializer.Deserialize<ReliefWebFuente.RespuestaReliefWeb>(json);
        var oferta = ReliefWebFuente.Convertir(respuesta!.Data![0]);

        Assert.Equal("reliefweb-456", oferta.Id);
        Assert.Equal("UNEP", oferta.Organizacion);
        Assert.Equal("Kenya", oferta.Ubicacion);
        Assert.Null(oferta.RegionPostulacion); // el país es el lugar de trabajo, no una restricción
    }

    [Fact]
    public void El_formato_escapa_caracteres_html()
    {
        var oferta = new Oferta("1", "Dev <senior> & climate", "Org", "", "https://x.com", "Test", null, "");
        var evaluada = new OfertaEvaluada(oferta, TipoOferta.DesarrolloConTema, ["climate"]);

        var texto = Formateador.FormatearOferta(evaluada);

        Assert.Contains("Dev &lt;senior&gt; &amp; climate", texto);
        Assert.StartsWith("💻🌿", texto);
    }

    [Fact]
    public void Muchas_ofertas_se_reparten_en_mensajes_que_no_superan_el_limite()
    {
        var titulo = new string('x', 500);
        var ofertas = Enumerable.Range(1, 20)
            .Select(i => new OfertaEvaluada(
                new Oferta($"{i}", titulo, "Org", "", "https://x.com", "Test", null, ""),
                TipoOferta.Tema, ["climate"]));

        var mensajes = Formateador.ArmarMensajes(ofertas);

        Assert.True(mensajes.Count > 1);
        Assert.All(mensajes, m => Assert.True(m.Length <= Formateador.LargoMaximoMensaje));
    }
}
