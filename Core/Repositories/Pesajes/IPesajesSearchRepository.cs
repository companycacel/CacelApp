using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Pesajes;

/// <summary>
/// Repositorio para operaciones de búsqueda y consulta de Pesajes
/// </summary>
public interface IPesajesSearchRepository
{
    /// <summary>
    /// Obtiene el listado de pesajes filtrado por tipo
    /// </summary>
    /// <param name="tipo">Tipo de pesaje (PE, PS, DS, etc.)</param>
    Task<ApiResponse<IEnumerable<Pes>>> GetPesajesAsync(string tipo);

    /// <summary>
    /// Obtiene un pesaje por su ID
    /// </summary>
    Task<ApiResponse<Pes>> GetPesajeByIdAsync(int id);

    /// <summary>
    /// Obtiene el detalle de pesajes (pde) para un pesaje específico
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalleAsync(int pesajeId);

    /// <summary>
    /// Obtiene listado de documentos de pesaje de la opción de Devolución
    /// </summary>
    Task<ApiResponse<IEnumerable<DocumentoPes>>> GetDocumentosAsync();
}
