using BuscadorOfertas.Filtros;
using BuscadorOfertas.Models;

namespace BuscadorOfertas.Tests;

public class FiltroInteresesTests
{
    private readonly FiltroIntereses _filtro = new();

    // Función auxiliar: crea una oferta de prueba. El título y el texto son iguales
    // salvo que se pase una descripción aparte.
    private static Oferta CrearOferta(string titulo, string? descripcion = null, string id = "1",
        DateTime? publicada = null, string? region = null) =>
        new(id, titulo, "Org", "Remoto", $"https://ejemplo.com/{id}", "Test", publicada,
            $"{titulo} {descripcion}", region);

    [Fact]
    public void Desarrollo_ambiental_es_la_categoria_mas_alta()
    {
        var resultado = _filtro.Evaluar(CrearOferta("Software developer for a climate data platform"));

        Assert.NotNull(resultado);
        Assert.Equal(TipoOferta.DesarrolloConTema, resultado.Tipo);
        Assert.Contains("climate", resultado.TemasEncontrados);
    }

    [Fact]
    public void Desarrollo_junior_sin_tema_se_incluye()
    {
        var resultado = _filtro.Evaluar(CrearOferta("Junior Frontend Developer"));

        Assert.NotNull(resultado);
        Assert.Equal(TipoOferta.DesarrolloJunior, resultado.Tipo);
    }

    [Fact]
    public void Desarrollo_senior_se_descarta_aunque_tenga_tema()
    {
        Assert.Null(_filtro.Evaluar(CrearOferta("Senior Developer for a climate startup")));
    }

    [Fact]
    public void Desarrollo_que_pide_muchos_anios_se_descarta()
    {
        var oferta = CrearOferta("Full Stack Developer", "You have 5+ years of experience with React.");

        Assert.Null(_filtro.Evaluar(oferta));
    }

    [Fact]
    public void Archivo_historico_es_relevante_aunque_no_sea_de_desarrollo()
    {
        var resultado = _filtro.Evaluar(CrearOferta("Archivist for historical documents digitization"));

        Assert.NotNull(resultado);
        Assert.Equal(TipoOferta.Tema, resultado.Tipo);
    }

    [Fact]
    public void Revision_de_contenido_abierta_a_todo_el_mundo_entra_como_flexible()
    {
        var oferta = CrearOferta("Content Reviewer", "No prior professional experience required.",
            region: "Worldwide");

        var resultado = _filtro.Evaluar(oferta);

        Assert.NotNull(resultado);
        Assert.Equal(TipoOferta.Flexible, resultado.Tipo);
    }

    [Fact]
    public void Revision_de_contenido_solo_para_USA_se_descarta()
    {
        var oferta = CrearOferta("Content Reviewer - United States", region: "USA");

        Assert.Null(_filtro.Evaluar(oferta));
    }

    [Fact]
    public void Oferta_que_no_encaja_en_ninguna_categoria_se_descarta()
    {
        Assert.Null(_filtro.Evaluar(CrearOferta("Account Executive", "Sell our SaaS product")));
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
        Assert.Equal(TipoOferta.DesarrolloConTema, resultado.Tipo);
    }

    [Fact]
    public void Se_ordenan_por_categoria()
    {
        var ofertas = new[]
        {
            CrearOferta("Content Reviewer", id: "flexible"),
            CrearOferta("Conservation officer", id: "tema"),
            CrearOferta("Frontend Developer", id: "dev"),
            CrearOferta("Junior Developer", id: "junior"),
            CrearOferta("C# .NET developer for biodiversity monitoring", id: "ideal"),
        };

        var resultado = _filtro.FiltrarYOrdenar(ofertas);

        Assert.Equal(
            new[] { "ideal", "junior", "dev", "tema", "flexible" },
            resultado.Select(r => r.Oferta.Id).ToArray());
    }

    [Fact]
    public void A_igual_categoria_va_primero_la_mas_nueva()
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
            CrearOferta("Climate developer", "Stack: React and Node", id: "a"),
            CrearOferta("Climate developer", "Stack: Python", id: "b"),
        };

        var resultado = _filtro.FiltrarYOrdenar(ofertas, "react node");

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
