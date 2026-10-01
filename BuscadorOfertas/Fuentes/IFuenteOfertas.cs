using BuscadorOfertas.Models;

namespace BuscadorOfertas.Fuentes;

/// <summary>
/// Contrato común para todas las fuentes de ofertas.
/// El bot solo conoce esta interfaz: no le importa si las ofertas
/// vienen de Remotive, de ReliefWeb o de una fuente falsa en un test.
/// </summary>
public interface IFuenteOfertas
{
    string Nombre { get; }

    Task<IReadOnlyList<Oferta>> ObtenerOfertasAsync(CancellationToken ct = default);
}
