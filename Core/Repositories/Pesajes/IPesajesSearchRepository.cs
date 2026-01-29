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
    Task<ApiResponse<Pes>> GetPesajeByIdAsync(int id);

    Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalleAsync(int pesajeId);
    Task<ApiResponse<IEnumerable<DocumentoPes>>> GetDocumentosAsync();
}
