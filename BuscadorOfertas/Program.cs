using BuscadorOfertas.Bot;
using BuscadorOfertas.Filtros;
using BuscadorOfertas.Fuentes;
using BuscadorOfertas.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

// ---------- Configuración ----------
var token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN")
    ?? throw new Exception("Falta la variable de entorno TELEGRAM_TOKEN");

// Un solo HttpClient para toda la app (crear uno por pedido agota las conexiones).
var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("BuscadorOfertasBot/1.0");

var fuentes = new List<IFuenteOfertas> { new RemotiveFuente(http) };

// ReliefWeb se activa solo cuando tengas el appname aprobado.
var appNameReliefWeb = Environment.GetEnvironmentVariable("RELIEFWEB_APPNAME");
if (!string.IsNullOrWhiteSpace(appNameReliefWeb))
    fuentes.Add(new ReliefWebFuente(http, appNameReliefWeb));

var filtro = new FiltroIntereses();
const int MaximoResultados = 10;

// ---------- Arranque del bot ----------
using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(token, cancellationToken: cts.Token);

var yo = await bot.GetMe();
Console.WriteLine($"@{yo.Username} funcionando con: {string.Join(", ", fuentes.Select(f => f.Nombre))}");
Console.WriteLine("Apretá Enter para cerrar.");

bot.OnError += (ex, origen) =>
{
    Console.WriteLine($"Error ({origen}): {ex.Message}");
    return Task.CompletedTask; // no hay nada que esperar, devolvemos una tarea ya terminada
};
bot.OnMessage += OnMessage;

Console.ReadLine();
cts.Cancel();

// ---------- Manejo de mensajes ----------
async Task OnMessage(Message msg, UpdateType tipo)
{
    if (msg.Text is not { } texto) return;

    // "/buscar clima remoto" → comando "/buscar", resto "clima remoto".
    // En grupos llega como "/buscar@MiBot", por eso cortamos en la "@".
    var partes = texto.Split(' ', 2, StringSplitOptions.TrimEntries);
    var comando = partes[0].Split('@')[0].ToLowerInvariant();
    var resto = partes.Length > 1 ? partes[1] : null;

    switch (comando)
    {
        case "/start":
        case "/ayuda":
            await bot.SendMessage(msg.Chat,
                "Busco ofertas sobre ambiente, naturaleza, impacto ambiental y documentación histórica, " +
                "y pongo primero las de desarrollo 💻.\n\n" +
                "/buscar → todas las ofertas que coinciden\n" +
                "/buscar palabras → además tienen que contener esas palabras (ej: /buscar remote)\n" +
                "/fuentes → de dónde saco las ofertas");
            break;

        case "/fuentes":
            await bot.SendMessage(msg.Chat,
                "Fuentes activas: " + string.Join(", ", fuentes.Select(f => f.Nombre)));
            break;

        case "/buscar":
            await Buscar(msg.Chat, resto);
            break;

        default:
            await bot.SendMessage(msg.Chat, "No conozco ese comando. Probá con /ayuda");
            break;
    }
}

async Task Buscar(Chat chat, string? palabrasExtra)
{
    await bot.SendMessage(chat, "Buscando… 🔍");

    // Consultamos todas las fuentes en paralelo. Si una falla, seguimos con las demás.
    var tareas = fuentes.Select(async fuente =>
    {
        try
        {
            return await fuente.ObtenerOfertasAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falló {fuente.Nombre}: {ex.Message}");
            return (IReadOnlyList<Oferta>)[];
        }
    });
    var resultados = await Task.WhenAll(tareas);

    var todas = resultados.SelectMany(r => r).ToList();
    var relevantes = filtro.FiltrarYOrdenar(todas, palabrasExtra);

    if (relevantes.Count == 0)
    {
        await bot.SendMessage(chat,
            $"Revisé {todas.Count} ofertas y ninguna coincide con tus temas por ahora. 🌱");
        return;
    }

    var deDesarrollo = relevantes.Count(r => r.EsDesarrollo);
    await bot.SendMessage(chat,
        $"Encontré {relevantes.Count} ofertas de {todas.Count} ({deDesarrollo} de desarrollo). " +
        $"Te muestro las primeras {Math.Min(MaximoResultados, relevantes.Count)}:");

    foreach (var mensaje in Formateador.ArmarMensajes(relevantes.Take(MaximoResultados)))
    {
        await bot.SendMessage(chat, mensaje,
            parseMode: ParseMode.Html,
            linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });
    }
}
