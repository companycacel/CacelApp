using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Pesajes;

/// <summary>
/// Interfaz para servicios de aplicación de Pesajes
/// Define las operaciones de lógica de negocio
/// </summary>
public interface IPesajesService
{
    /// <summary>
    /// Obtiene el listado de pesajes filtrado por tipo
    /// </summary>
    Task<ApiResponse<IEnumerable<Pes>>> GetPesajes(string pes_tipo);

    /// <summary>
    /// Obtiene un pesaje por su ID
    /// </summary>
    Task<ApiResponse<Pes>> GetPesajesById(int code);

    /// <summary>
    /// Obtiene el reporte en PDF de un pesaje
    /// </summary>
    Task<byte[]> GetReportAsync(int code);

    /// <summary>
    /// Crea o actualiza un pesaje
    /// </summary>
    Task<ApiResponse<Pes>> Pesajes(Pes request);

    /// <summary>
    /// Obtiene el detalle de pesajes para un pesaje específico
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalle(int code);

    /// <summary>
    /// Crea o actualiza un detalle de pesaje
    /// </summary>
    Task<ApiResponse<Pde>> PesajesDetalle(Pde request);

    /// <summary>
    /// Obtiene la descripción del estado del pesaje
    /// </summary>
    string GetStatusDescription(int status);

    /// <summary>
    /// Valida si un pesaje puede ser editado según su tipo
    /// </summary>
    bool CanEdit(string pes_tipo);

    /// <summary>
    /// Valida si un pesaje puede ser anulado según su estado
    /// </summary>
    bool CanDelete(int pes_status);
}
