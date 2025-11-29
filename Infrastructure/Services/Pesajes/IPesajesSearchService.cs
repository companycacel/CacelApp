using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Infrastructure.Services.Pesajes;

/// <summary>
/// Servicio para operaciones de lectura y búsqueda de Pesajes
/// </summary>
public interface IPesajesSearchService
{
    /// <summary>
    /// Busca pesajes por tipo
    /// </summary>
    /// <param name="tipo">PE (Entrada), PS (Salida), DS (Devolución)</param>
    Task<ApiResponse<IEnumerable<Pes>>> SearchPesajesAsync(string tipo);

    /// <summary>
    /// Obtiene un pesaje específico por ID
    /// </summary>
    Task<ApiResponse<Pes>> GetPesajeByIdAsync(int id);

    /// <summary>
    /// Obtiene los detalles de un pesaje
    /// </summary>
    Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalleAsync(int pesajeId);

    /// <summary>
    /// Obtiene el listado de documentos de pesaje
    /// </summary>
    Task<ApiResponse<IEnumerable<DocumentoPes>>> GetDocumentosAsync();

    /// <summary>
    /// Genera el reporte PDF de un pesaje
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(int id);

    /// <summary>
    /// Obtiene la descripción del estado del pesaje
    /// </summary>
    string GetStatusDescription(int status);

    /// <summary>
    /// Valida si un pesaje puede ser editado según su tipo
    /// </summary>
    bool CanEdit(string tipo);

    /// <summary>
    /// Valida si un pesaje puede ser anulado según su estado
    /// </summary>
    bool CanDelete(int status);
}
