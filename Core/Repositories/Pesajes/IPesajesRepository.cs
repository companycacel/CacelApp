using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Pesajes;

/// <summary>
/// Repositorio para operaciones relacionadas con Pesajes
/// </summary>
public interface IPesajesRepository
{
    /// <summary>
    /// Obtiene el listado de pesajes filtrado por tipo
    /// </summary>
    /// <param name="pes_tipo">Tipo de pesaje (PE, PS, DS, etc.)</param>
    /// <returns>Lista de pesajes</returns>
    Task<ApiResponse<IEnumerable<Pes>>> GetPesajes(string pes_tipo);

    /// <summary>
    /// Obtiene un pesaje por su ID
    /// </summary>
    /// <param name="id">ID del pesaje</param>
    /// <returns>Pesaje encontrado</returns>
    Task<ApiResponse<Pes>> GetPesajesById(int id);

    /// <summary>
    /// Obtiene el reporte en PDF de un pesaje
    /// </summary>
    /// <param name="code">ID del pesaje</param>
    /// <returns>Bytes del PDF</returns>
    Task<byte[]> GetReportAsync(int code);

    /// <summary>
    /// Crea o actualiza un pesaje
    /// </summary>
    /// <param name="request">Datos del pesaje</param>
    /// <returns>Respuesta con el pesaje procesado</returns>
    Task<ApiResponse<Pes>> Pesajes(Pes request);

    /// <summary>
    /// Obtiene el detalle de pesajes (pde) para un pesaje específico
    /// </summary>
    /// <param name="code">ID del pesaje</param>
    /// <returns>Lista de detalles</returns>
    Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalle(int code);

    /// <summary>
    /// Crea o actualiza un detalle de pesaje
    /// </summary>
    /// <param name="request">Datos del detalle</param>
    /// <returns>Respuesta con el detalle procesado</returns>
    Task<ApiResponse<Pde>> PesajesDetalle(Pde request);

    /// <summary>
    /// retorna listado de documentos de pesaje de la Opcion de Devolucion
    /// </summary>
    Task<ApiResponse<IEnumerable<DocumentoPes>>> document();

}
