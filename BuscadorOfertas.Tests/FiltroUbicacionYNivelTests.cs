using BuscadorOfertas.Filtros;

namespace BuscadorOfertas.Tests;

public class FiltroUbicacionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Worldwide")]
    [InlineData("Anywhere in the World")]
    [InlineData("LATAM")]
    [InlineData("Latin America")]
    [InlineData("USA, Canada, Latin America")]
    [InlineData("Americas")]
    [InlineData("Argentina")]
    [InlineData("UTC-5 to UTC+1")]
    [InlineData("GMT-3 - GMT+2")]
    public void Regiones_donde_puedo_postularme(string? region)
    {
        Assert.True(FiltroUbicacion.PuedoPostularme(region));
    }

    [Theory]
    [InlineData("USA")]
    [InlineData("USA Only")]
    [InlineData("Europe")]
    [InlineData("UK, Germany")]
    [InlineData("North America")]
    [InlineData("UTC+0 to UTC+3")]
    public void Regiones_donde_no_puedo_postularme(string region)
    {
        Assert.False(FiltroUbicacion.PuedoPostularme(region));
    }
}

public class FiltroNivelTests
{
    [Theory]
    [InlineData("Senior Backend Engineer")]
    [InlineData("Sr. Developer")]
    [InlineData("Tech Lead")]
    [InlineData("Staff Software Engineer")]
    [InlineData("Engineering Manager")]
    public void Titulos_senior(string titulo)
    {
        Assert.True(FiltroNivel.EsSenior(titulo));
    }

    [Theory]
    [InlineData("Junior Developer")]
    [InlineData("Headless CMS Developer")] // "head" dentro de "headless" no cuenta
    [InlineData("International Support Developer")] // "intern" dentro de "international" tampoco
    public void Titulos_que_no_son_senior(string titulo)
    {
        Assert.False(FiltroNivel.EsSenior(titulo));
    }

    [Theory]
    [InlineData("you have 5+ years with react", 5)]
    [InlineData("3-5 years of experience", 3)]
    [InlineData("at least 4 years in web development", 4)]
    [InlineData("2 years of professional experience", 2)]
    [InlineData("3 años de experiencia en .net", 3)]
    [InlineData("de 2 a 4 años de experiencia", 2)]
    [InlineData("1+ years with react and 4+ years overall", 4)]
    public void Detecta_los_anios_de_experiencia(string texto, int esperado)
    {
        Assert.Equal(esperado, FiltroNivel.AniosPedidos(FiltroTexto.Normalizar(texto)));
    }

    [Theory]
    [InlineData("we have been around for 20 years")]
    [InlineData("no experience mentioned here")]
    public void Sin_requisito_de_anios(string texto)
    {
        Assert.Null(FiltroNivel.AniosPedidos(FiltroTexto.Normalizar(texto)));
    }

    [Fact]
    public void Lee_anios_aunque_la_descripcion_venga_en_html()
    {
        var html = "<li>8+&nbsp;years writing <b>frontend</b> code</li>";

        Assert.Equal(8, FiltroNivel.AniosPedidos(FiltroTexto.Normalizar(html)));
    }
}
