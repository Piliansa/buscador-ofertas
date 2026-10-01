# Buscador de Ofertas 🌿💻

Bot de Telegram en C# que busca ofertas laborales **a las que me puedo postular desde Argentina** y **a mi nivel (junior)**, priorizando desarrollo de software en temas de **ambiente, naturaleza, impacto ambiental y documentación histórica**.

## Qué ofertas muestra

Una oferta aparece solo si cumple las tres reglas:

1. **Ubicación** (`FiltroUbicacion`): acepta postulantes desde Argentina ("Worldwide", "LATAM", "Americas", "Argentina" o un rango horario que incluya UTC-3).
2. **Nivel** (`FiltroNivel`): el título no es senior/lead/staff y no pide más de 2 años de experiencia.
3. **Categoría** (`FiltroIntereses`), en este orden de prioridad:

| | Categoría |
|---|---|
| 💻🌿 | Desarrollo en ambiente o historia |
| 💻🌱 | Desarrollo junior |
| 💻 | Desarrollo |
| 🌿 | Ambiente o historia (sin desarrollo) |
| 🧩 | Trabajos flexibles: revisión de contenido, evaluación de IA, etiquetado de datos… |

## Estructura

```
BuscadorOfertas/
├── Models/Oferta.cs              ← record con los datos de una oferta
├── Fuentes/IFuenteOfertas.cs     ← interfaz común a todas las fuentes
├── Fuentes/RemotiveFuente.cs     ← trabajos remotos (API pública, sin registro)
├── Fuentes/ReliefWebFuente.cs    ← ONGs y ONU (necesita appname aprobado)
├── Filtros/FiltroIntereses.cs    ← categorías y orden de prioridad
├── Filtros/FiltroUbicacion.cs    ← ¿me puedo postular desde Argentina?
├── Filtros/FiltroNivel.cs        ← ¿es de mi nivel?
├── Filtros/FiltroTexto.cs        ← normaliza texto y arma patrones de búsqueda
├── Bot/Formateador.cs            ← arma los mensajes de Telegram
└── Program.cs                    ← comandos del bot
BuscadorOfertas.Tests/            ← tests con xUnit
```

## Cómo correrlo

1. Instalá el [.NET SDK 10](https://dotnet.microsoft.com/download).
2. Pedile un token a **@BotFather** en Telegram (`/newbot`).
3. Desde la carpeta raíz, cargá el token sin escribirlo en el comando (así no queda guardado en el historial de la terminal) y corré el bot:

```powershell
# Windows (PowerShell)
$env:TELEGRAM_TOKEN = Read-Host "Pegá el token"
dotnet run --project BuscadorOfertas
```

```bash
# macOS / Linux
read -s -p "Pegá el token: " TELEGRAM_TOKEN && export TELEGRAM_TOKEN
dotnet run --project BuscadorOfertas
```

La variable dura solo mientras la terminal está abierta: no se guarda en ningún archivo ni se sube al repo. **Nunca pongas el token en el código ni lo compartas.** Si se llega a filtrar, generá uno nuevo con `/revoke` en @BotFather.

4. En Telegram, escribile a tu bot `/start` y después `/buscar`.

### Activar ReliefWeb (empleos de ONGs)

Pedí un appname gratis completando [este formulario](https://docs.google.com/forms/d/e/1FAIpQLScR5EE_SBhweLLg_2xMCnXNbT6md4zxqIB00OL0yZWyrqX_Nw/viewform) (no hace falta crear cuenta). El nombre tiene que combinar tu nombre, el propósito y caracteres al azar, por ejemplo `tunombre-buscadorofertas-k7f3x`. Cuando te lo aprueben:

```bash
export RELIEFWEB_APPNAME="tu-appname"            # macOS / Linux
$env:RELIEFWEB_APPNAME = "tu-appname"          # Windows (PowerShell)
```

Si la variable no está, el bot funciona solo con Remotive.

## Comandos

| Comando | Qué hace |
|---|---|
| `/buscar` | Todas las ofertas que pasan los filtros |
| `/buscar react` | Además tienen que contener esas palabras |
| `/fuentes` | Muestra qué fuentes están activas |
| `/ayuda` | Ayuda |

## Tests

```bash
dotnet test
```

Los tests prueban cada filtro (ubicación, nivel, categorías y falsos positivos), la lectura del JSON de cada fuente y el armado de mensajes, sin conectarse a internet.

## Ajustar los filtros

Las palabras clave de temas, desarrollo y trabajos flexibles están al principio de `Filtros/FiltroIntereses.cs`; las de nivel y el máximo de años, en `Filtros/FiltroNivel.cs`; las regiones permitidas, en `Filtros/FiltroUbicacion.cs`. Si agregás una palabra, sumá un test que lo confirme. Cuidado con las palabras muy comunes: `environment` aparece en "fast-paced environment" y `nature` en "the nature of the role", por eso se usan términos más específicos.

## Próximas etapas

- [x] Filtro por ubicación y por nivel junior
- [ ] Guardar filtros por usuario y ofertas ya vistas (JSON → SQLite)
- [ ] Aviso automático de ofertas nuevas con `PeriodicTimer`
- [ ] Más fuentes (Himalayas, Arbeitnow…)
- [ ] Desplegarlo para que corra siempre

## Créditos

Ofertas de [Remotive](https://remotive.com) y [ReliefWeb](https://reliefweb.int). Cada oferta enlaza a su publicación original.
