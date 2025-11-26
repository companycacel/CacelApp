using Core.Repositories.Balanza.Entities;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Interfaz para el servicio de lectura de balanza
/// Define operaciones de consulta y búsqueda de registros
/// </summary>
public interface IBalanzaSearchService
{
    /// <summary>
    /// Obtiene registros de balanza con filtros opcionales
    /// </summary>
    Task<IEnumerable<Baz>> ObtenerRegistrosAsync(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? vehiculoId,
        string? Agente,
        int? estado,
        CancellationToken cancellationToken = default);
}
