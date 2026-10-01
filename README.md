# Buscador de Ofertas 🌿💻

Bot de Telegram en C# que busca ofertas laborales sobre **ambiente, naturaleza, impacto ambiental y documentación histórica**, y muestra primero las de **desarrollo de software**.

## Estructura

```
BuscadorOfertas/
├── Models/Oferta.cs              ← record con los datos de una oferta
├── Fuentes/IFuenteOfertas.cs     ← interfaz común a todas las fuentes
├── Fuentes/RemotiveFuente.cs     ← trabajos remotos (API pública, sin registro)
├── Fuentes/ReliefWebFuente.cs    ← ONGs y ONU (necesita appname aprobado)
├── Filtros/FiltroIntereses.cs    ← decide qué es interesante y en qué orden
├── Bot/Formateador.cs            ← arma los mensajes de Telegram
└── Program.cs                    ← comandos del bot
BuscadorOfertas.Tests/            ← tests con xUnit
```

## Cómo correrlo

1. Instalá el [.NET SDK 8](https://dotnet.microsoft.com/download) o más nuevo.
2. Pedile un token a **@BotFather** en Telegram (`/newbot`).
3. Desde la carpeta raíz:

```bash
# macOS / Linux
export TELEGRAM_TOKEN="tu-token"
dotnet run --project BuscadorOfertas

# Windows (PowerShell)
$env:TELEGRAM_TOKEN="tu-token"
dotnet run --project BuscadorOfertas
```

4. En Telegram, escribile a tu bot `/start` y después `/buscar`.

### Activar ReliefWeb (empleos de ONGs)

Pedí un appname gratis en https://reliefweb.int/contact (describilo como herramienta personal que consulta la API de empleos). Cuando te lo aprueben:

```bash
export RELIEFWEB_APPNAME="tu-appname"
```

Si la variable no está, el bot funciona solo con Remotive.

## Comandos

| Comando | Qué hace |
|---|---|
| `/buscar` | Todas las ofertas que coinciden con tus temas |
| `/buscar remote latam` | Además tienen que contener esas palabras |
| `/fuentes` | Muestra qué fuentes están activas |
| `/ayuda` | Ayuda |

## Tests

```bash
dotnet test
```

Los tests prueban el filtro (incluidos los falsos positivos), la lectura del JSON de cada fuente y el armado de mensajes, sin conectarse a internet.

## Cambiar los temas

Las palabras clave están al principio de `Filtros/FiltroIntereses.cs`. Si agregás una palabra, sumá un test que lo confirme. Cuidado con las palabras muy comunes: `environment` aparece en "fast-paced environment" y `nature` en "the nature of the role", por eso se usan términos más específicos.

## Próximas etapas

- [ ] Guardar filtros por usuario y ofertas ya vistas (JSON → SQLite)
- [ ] Aviso automático de ofertas nuevas con `PeriodicTimer`
- [ ] Más fuentes (Himalayas, Arbeitnow…)
- [ ] Desplegarlo para que corra siempre

## Créditos

Ofertas de [Remotive](https://remotive.com) y [ReliefWeb](https://reliefweb.int). Cada oferta enlaza a su publicación original.
