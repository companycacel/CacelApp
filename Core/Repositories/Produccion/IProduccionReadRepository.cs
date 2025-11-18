using Core.Shared.Entities.Generic;

namespace Core.Repositories.Produccion;

/// <summary>
/// Interfaz que define el contrato para operaciones de lectura de producción
/// Implementa el patrón Repository y separación de responsabilidades (CQRS)
/// </summary>
public interface IProduccionReadRepository
{
    /// <summary>
    /// Obtiene todos los registros de producción con filtros opcionales
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio del filtro</param>
    /// <param name="fechaFin">Fecha fin del filtro</param>
    /// <param name="materialId">ID del material para filtrar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de registros de producción</returns>
    Task<IEnumerable<Pde>> ObtenerTodosAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        int? materialId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un registro de producción por su ID
    /// </summary>
    /// <param name="id">ID del registro de producción</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Registro de producción encontrado</returns>
    Task<Pde?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el reporte PDF de un pesaje de producción
    /// </summary>
    /// <param name="pesajeId">ID del pesaje</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Bytes del PDF</returns>
    Task<byte[]> ObtenerReporteAsync(int pesajeId, CancellationToken cancellationToken = default);
}
