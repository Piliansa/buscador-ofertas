using System.Net;
using System.Text;
using BuscadorOfertas.Filtros;

namespace BuscadorOfertas.Bot;

/// <summary>
/// Arma los textos que manda el bot. Está separado de Program.cs
/// para poder testearlo sin conectarse a Telegram.
/// </summary>
public static class Formateador
{
    // Telegram corta los mensajes en 4096 caracteres; dejamos margen.
    public const int LargoMaximoMensaje = 3800;

    public static string Icono(TipoOferta tipo) => tipo switch
    {
        TipoOferta.DesarrolloConTema => "💻🌿",
        TipoOferta.DesarrolloJunior => "💻🌱",
        TipoOferta.Desarrollo => "💻",
        TipoOferta.Tema => "🌿",
        TipoOferta.Flexible => "🧩",
        _ => "•"
    };

    public static string Descripcion(TipoOferta tipo) => tipo switch
    {
        TipoOferta.DesarrolloConTema => "Desarrollo en tu tema",
        TipoOferta.DesarrolloJunior => "Desarrollo junior",
        TipoOferta.Desarrollo => "Desarrollo",
        TipoOferta.Tema => "Tu tema",
        TipoOferta.Flexible => "Trabajo flexible",
        _ => ""
    };

    public static string FormatearOferta(OfertaEvaluada e)
    {
        var o = e.Oferta;
        var sb = new StringBuilder();

        // Escapamos el texto porque mandamos el mensaje en modo HTML:
        // un "<" en un título rompería el formato.
        sb.Append($"{Icono(e.Tipo)} <b>{Esc(o.Titulo)}</b>\n");

        if (!string.IsNullOrWhiteSpace(o.Organizacion))
            sb.Append($"🏢 {Esc(o.Organizacion)}\n");
        if (!string.IsNullOrWhiteSpace(o.Ubicacion))
            sb.Append($"📍 {Esc(o.Ubicacion)}\n");

        var motivo = Descripcion(e.Tipo);
        if (e.TemasEncontrados.Count > 0)
            motivo += $" ({string.Join(", ", e.TemasEncontrados.Take(4))})";
        sb.Append($"🔎 {Esc(motivo)}\n");

        sb.Append($"<a href=\"{Esc(o.Url)}\">Ver oferta en {Esc(o.Fuente)}</a>");
        return sb.ToString();
    }

    /// <summary>Junta varias ofertas en la menor cantidad de mensajes posible.</summary>
    public static List<string> ArmarMensajes(IEnumerable<OfertaEvaluada> ofertas)
    {
        var mensajes = new List<string>();
        var actual = new StringBuilder();

        foreach (var bloque in ofertas.Select(FormatearOferta))
        {
            if (actual.Length > 0 && actual.Length + bloque.Length + 2 > LargoMaximoMensaje)
            {
                mensajes.Add(actual.ToString());
                actual.Clear();
            }
            if (actual.Length > 0) actual.Append("\n\n");
            actual.Append(bloque);
        }

        if (actual.Length > 0) mensajes.Add(actual.ToString());
        return mensajes;
    }

    private static string Esc(string texto) => WebUtility.HtmlEncode(texto);
}
