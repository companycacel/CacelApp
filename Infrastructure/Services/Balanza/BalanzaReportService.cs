using Core.Repositories.Balanza;
using Core.Shared.Validators;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Servicio de aplicación para generación de reportes de balanza
/// Implementa la lógica de negocio para reportes y estadísticas
/// </summary>
public class BalanzaReportService : IBalanzaReportService
{
    private readonly IBalanzaReportRepository _repository;

    public BalanzaReportService(IBalanzaReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarId(registroId, nameof(registroId));
        return await _repository.GenerarReportePdfAsync(registroId, cancellationToken);
    }

    public async Task<byte[]> GenerarReporteExcelAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        string? vehiculoId,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarRangoFechas(fechaInicio, fechaFin);
        return await _repository.GenerarReporteExcelAsync(fechaInicio, fechaFin, vehiculoId, cancellationToken);
    }

    public async Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarRangoFechas(fechaInicio, fechaFin);
        return await _repository.ObtenerEstadisticasAsync(fechaInicio, fechaFin, cancellationToken);
    }
}
