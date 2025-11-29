using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Produccion;

/// <summary>
/// Servicio de aplicación para operaciones de lectura y búsqueda de Producción
/// </summary>
public class ProduccionSearchService : IProduccionSearchService
{
    private readonly IProduccionSearchRepository _searchRepository;
    private readonly IProduccionReportRepository _reportRepository;

    public ProduccionSearchService(
        IProduccionSearchRepository searchRepository,
        IProduccionReportRepository reportRepository)
    {
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
        _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
    }

    /// <summary>
    /// Obtiene la lista de registros de producción con filtros
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> SearchProduccionAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null)
    {
        return await _searchRepository.GetProduccionAsync(fechaInicio, fechaFin, materialId);
    }

    /// <summary>
    /// Genera el reporte PDF de un registro de producción
    /// </summary>
    public async Task<byte[]> GenerateReportPdfAsync(int id)
    {
        ValidationHelper.ValidarId(id, nameof(id));
        return await _reportRepository.GenerateReportPdfAsync(id);
    }
}
