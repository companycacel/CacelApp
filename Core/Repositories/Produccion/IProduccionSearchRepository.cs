using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Produccion;

/// <summary>
/// Repositorio para operaciones de búsqueda y consulta de Producción
/// </summary>
public interface IProduccionSearchRepository
{
    /// <summary>
    /// Obtiene la lista de registros de producción con filtros
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> GetProduccionAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null);
}
