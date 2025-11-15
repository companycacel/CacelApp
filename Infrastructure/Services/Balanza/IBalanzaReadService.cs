using Core.Repositories.Balanza.Entities;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Interfaz para el servicio de lectura de balanza
/// Define operaciones de consulta y búsqueda de registros
/// </summary>
public interface IBalanzaReadService
{
    /// <summary>
    /// Obtiene registros de balanza con filtros opcionales
    /// </summary>
    Task<IEnumerable<Baz>> ObtenerRegistrosAsync(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? vehiculoId,
        string? agenciaDescripcion,
        int? estado,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un registro específico por su ID
    /// </summary>
    Task<Baz?> ObtenerRegistroPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene registros de un vehículo específico en un rango de fechas
    /// </summary>
    Task<IEnumerable<Baz>> ObtenerRegistrosPorVehiculoAsync(
        string vehiculoId,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        CancellationToken cancellationToken = default);
}
