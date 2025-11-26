using Core.Repositories.Balanza.Entities;

namespace Core.Repositories.Balanza;

/// <summary>
/// Interfaz que define el contrato para operaciones de lectura de registros de balanza
/// Implementa el patrón Repository y separación de responsabilidades
/// </summary>
public interface IBalanzaSearchRepository
{
    /// <summary>
    /// Obtiene todos los registros de balanza con filtros opcionales
    /// </summary>
    Task<IEnumerable<Baz>> ObtenerTodosAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? vehiculoId = null,
        string? Agente = null,
        int? estado = null,
        CancellationToken cancellationToken = default);
}
