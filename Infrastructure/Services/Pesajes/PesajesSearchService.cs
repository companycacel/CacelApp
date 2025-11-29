using Core.Repositories.Pesajes;
using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Pesajes;

/// <summary>
/// Servicio de aplicación para operaciones de lectura y búsqueda de Pesajes
/// </summary>
public class PesajesSearchService : IPesajesSearchService
{
    private readonly IPesajesSearchRepository _searchRepository;
    private readonly IPesajesReportRepository _reportRepository;

    public PesajesSearchService(
        IPesajesSearchRepository searchRepository,
        IPesajesReportRepository reportRepository)
    {
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
    }

    /// <summary>
    /// Busca pesajes por tipo
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pes>>> SearchPesajesAsync(string tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            throw new ArgumentException("El tipo de pesaje no puede estar vacío", nameof(tipo));

        return await _searchRepository.GetPesajesAsync(tipo);
    }

    /// <summary>
    /// Obtiene un pesaje específico por ID
    /// </summary>
    public async Task<ApiResponse<Pes>> GetPesajeByIdAsync(int id)
    {
        ValidationHelper.ValidarId(id, nameof(id));
        return await _searchRepository.GetPesajeByIdAsync(id);
    }

    /// <summary>
    /// Obtiene los detalles de un pesaje
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalleAsync(int pesajeId)
    {
        ValidationHelper.ValidarId(pesajeId, nameof(pesajeId));
        return await _searchRepository.GetPesajesDetalleAsync(pesajeId);
    }

    /// <summary>
    /// Obtiene el listado de documentos de pesaje
    /// </summary>
    public async Task<ApiResponse<IEnumerable<DocumentoPes>>> GetDocumentosAsync()
    {
        return await _searchRepository.GetDocumentosAsync();
    }

    /// <summary>
    /// Genera el reporte PDF de un pesaje
    /// </summary>
    public async Task<byte[]> GenerateReportPdfAsync(int id)
    {
        ValidationHelper.ValidarId(id, nameof(id));
        return await _reportRepository.GenerateReportPdfAsync(id);
    }

    /// <summary>
    /// Obtiene la descripción del estado del pesaje
    /// </summary>
    public string GetStatusDescription(int status)
    {
        return status switch
        {
            1 => "PROCESADO",
            2 => "REGISTRANDO",
            _ => "DESCONOCIDO"
        };
    }

    /// <summary>
    /// Valida si un pesaje puede ser editado según su tipo
    /// </summary>
    public bool CanEdit(string tipo)
    {
        return new[] { "PE", "PS", "DS" }.Contains(tipo);
    }

    /// <summary>
    /// Valida si un pesaje puede ser anulado según su estado
    /// </summary>
    public bool CanDelete(int status)
    {
        return status == 2; // Solo se pueden anular pesajes en estado REGISTRANDO
    }
}
