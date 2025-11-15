using Core.Domain.Balanza.Entities;

namespace Core.Repositories.Balanza;

/// <summary>
/// Interfaz que define el contrato para operaciones de lectura de registros de balanza
/// Implementa el patrón Repository y separación de responsabilidades
/// </summary>
public interface IBalanzaReadRepository
{
    /// <summary>
    /// Obtiene todos los registros de balanza con filtros opcionales
    /// </summary>
    Task<IEnumerable<Domain.Balanza.Entities.Baz>> ObtenerTodosAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? vehiculoId = null,
        string? agenciaDescripcion = null,
        int? estado = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un registro de balanza por su ID
    /// </summary>
    Task<Domain.Balanza.Entities.Baz?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene registros de balanza para un vehículo específico
    /// </summary>
    Task<IEnumerable<Domain.Balanza.Entities.Baz>> ObtenerPorVehiculoAsync(
        string vehiculoId,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        CancellationToken cancellationToken = default);
}
