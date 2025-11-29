using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Infrastructure.Services.Produccion;

/// <summary>
/// Servicio para operaciones de lectura y búsqueda de Producción
/// </summary>
public interface IProduccionSearchService
{
    /// <summary>
    /// Obtiene la lista de registros de producción con filtros
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> SearchProduccionAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null);

    /// <summary>
    /// Genera el reporte PDF de un registro de producción
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(int id);
}
