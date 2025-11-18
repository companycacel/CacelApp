using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Produccion;

/// <summary>
/// Interfaz para servicios de aplicación de Producción
/// Define las operaciones de lógica de negocio
/// </summary>
public interface IProduccionService
{
    /// <summary>
    /// Obtiene la lista de registros de producción con filtros
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> GetProduccion(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null);

    /// <summary>
    /// Obtiene un registro de producción por su ID
    /// </summary>
    Task<ApiResponse<Pde>> GetProduccionById(int code);

    /// <summary>
    /// Obtiene el reporte en PDF de un registro de producción
    /// </summary>
    Task<byte[]> GetReportAsync(int code);

    /// <summary>
    /// Crea o actualiza un registro de producción
    /// </summary>
    Task<ApiResponse<Pde>> Produccion(Pde request);

    /// <summary>
    /// Obtiene el detalle de producción para un pesaje específico
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> GetProduccionDetalle(int code);

    /// <summary>
    /// Crea o actualiza un detalle de producción
    /// </summary>
    Task<ApiResponse<Pde>> ProduccionDetalle(Pde request);
}
