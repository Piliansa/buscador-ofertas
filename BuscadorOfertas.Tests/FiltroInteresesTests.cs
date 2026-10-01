using BuscadorOfertas.Filtros;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Tests;

public class FiltroInteresesTests
{
    private readonly FiltroIntereses _filtro = new();

    // Función auxiliar: crea una oferta de prueba con solo el texto que nos importa.
    private static Oferta CrearOferta(string texto, string id = "1", DateTime? publicada = null) =>
        new(id, texto, "Org", "Remoto", $"https://ejemplo.com/{id}", "Test", publicada, texto);

    [Fact]
    public void Oferta_de_desarrollo_ambiental_es_relevante_y_de_desarrollo()
    {
        var oferta = CrearOferta("Software developer for a climate data platform");

        var resultado = _filtro.Evaluar(oferta);

        Assert.NotNull(resultado);
        Assert.True(resultado.EsDesarrollo);
        Assert.Contains("climate", resultado.TemasEncontrados);
    }

    [Fact]
    public void Oferta_de_desarrollo_sin_tema_se_descarta()
    {
        var oferta = CrearOferta("Senior backend developer for a fintech startup");

        Assert.Null(_filtro.Evaluar(oferta));
    }

    [Fact]
    public void Archivo_historico_es_relevante_aunque_no_sea_de_desarrollo()
    {
        var oferta = CrearOferta("Archivist for historical documents digitization");

        var resultado = _filtro.Evaluar(oferta);

        Assert.NotNull(resultado);
        Assert.False(resultado.EsDesarrollo);
    }

    [Theory]
    [InlineData("We offer a fast-paced environment and a great team")]
    [InlineData("Explain the nature of the role to stakeholders")]
    [InlineData("Our chef makes the best carbonara in town")]
    public void Palabras_ambiguas_no_dan_falsos_positivos(string texto)
    {
        Assert.Null(_filtro.Evaluar(CrearOferta(texto)));
    }

    [Theory]
    [InlineData("Desarrolladora para proyecto de impacto ambiental")]
    [InlineData("DESARROLLADORA PARA PROYECTO DE IMPACTO AMBIENTAL")]
    [InlineData("Programadora: digitalización de patrimonio histórico")]
    public void Funciona_en_castellano_con_mayusculas_y_tildes(string texto)
    {
        var resultado = _filtro.Evaluar(CrearOferta(texto));

        Assert.NotNull(resultado);
        Assert.True(resultado.EsDesarrollo);
    }

    [Fact]
    public void Las_de_desarrollo_van_primero()
    {
        var ofertas = new[]
        {
            CrearOferta("Conservation officer", id: "a"),
            CrearOferta("C# .NET developer for biodiversity monitoring", id: "b"),
        };

        var resultado = _filtro.FiltrarYOrdenar(ofertas);

        Assert.Equal(2, resultado.Count);
        Assert.Equal("b", resultado[0].Oferta.Id);
    }

    [Fact]
    public void A_igual_relevancia_va_primero_la_mas_nueva()
    {
        var ofertas = new[]
        {
            CrearOferta("Climate developer", id: "vieja", publicada: new DateTime(2026, 1, 1)),
            CrearOferta("Climate developer", id: "nueva", publicada: new DateTime(2026, 9, 1)),
        };

        var resultado = _filtro.FiltrarYOrdenar(ofertas);

        Assert.Equal("nueva", resultado[0].Oferta.Id);
    }

    [Fact]
    public void Palabras_extra_tienen_que_aparecer_todas()
    {
        var ofertas = new[]
        {
            CrearOferta("Climate developer, remote in Latin America", id: "a"),
            CrearOferta("Climate developer, on-site in Berlin", id: "b"),
        };

        var resultado = _filtro.FiltrarYOrdenar(ofertas, "remote latin");

        Assert.Single(resultado);
        Assert.Equal("a", resultado[0].Oferta.Id);
    }

    [Fact]
    public void Ofertas_repetidas_con_la_misma_url_aparecen_una_vez()
    {
        var oferta = CrearOferta("Forest data developer");

        var resultado = _filtro.FiltrarYOrdenar([oferta, oferta with { Id = "otra" }]);

        Assert.Single(resultado);
    }
}
