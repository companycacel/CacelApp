using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Produccion;

/// <summary>
/// Repositorio para operaciones relacionadas con Producción
/// </summary>
public interface IProduccionRepository
{
    /// <summary>
    /// Obtiene la lista de registros de producción con filtros
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio opcional</param>
    /// <param name="fechaFin">Fecha de fin opcional</param>
    /// <param name="materialId">ID del material opcional</param>
    /// <returns>Lista de registros de producción</returns>
    Task<ApiResponse<IEnumerable<Pde>>> GetProduccion(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null);

    /// <summary>
    /// Obtiene un registro de producción por su ID
    /// </summary>
    /// <param name="id">ID del registro</param>
    /// <returns>Registro de producción encontrado</returns>
    Task<ApiResponse<Pde>> GetProduccionById(int id);

    /// <summary>
    /// Obtiene el reporte en PDF de un registro de producción
    /// </summary>
    /// <param name="code">ID del registro</param>
    /// <returns>Bytes del PDF</returns>
    Task<byte[]> GetReportAsync(int code);

    /// <summary>
    /// Crea o actualiza un registro de producción
    /// </summary>
    /// <param name="request">Datos del registro</param>
    /// <returns>Respuesta con el registro procesado</returns>
    Task<ApiResponse<Pde>> Produccion(Pde request);

    /// <summary>
    /// Obtiene el detalle de producción para un pesaje específico
    /// </summary>
    /// <param name="code">ID del pesaje</param>
    /// <returns>Lista de detalles</returns>
    Task<ApiResponse<IEnumerable<Pde>>> GetProduccionDetalle(int code);

    /// <summary>
    /// Crea o actualiza un detalle de producción
    /// </summary>
    /// <param name="request">Datos del detalle</param>
    /// <returns>Respuesta con el detalle procesado</returns>
    Task<ApiResponse<Pde>> ProduccionDetalle(Pde request);
}
