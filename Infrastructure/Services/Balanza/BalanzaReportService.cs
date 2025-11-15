using Core.Repositories.Balanza;

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
        if (registroId <= 0)
            throw new ArgumentException("El ID del registro debe ser válido", nameof(registroId));

        return await _repository.GenerarReportePdfAsync(registroId, cancellationToken);
    }

    public async Task<byte[]> GenerarReporteExcelAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        string? vehiculoId,
        CancellationToken cancellationToken = default)
    {
        if (fechaInicio > fechaFin)
            throw new InvalidOperationException("La fecha de inicio no puede ser mayor a la fecha de fin");

        return await _repository.GenerarReporteExcelAsync(fechaInicio, fechaFin, vehiculoId, cancellationToken);
    }

    public async Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        if (fechaInicio > fechaFin)
            throw new InvalidOperationException("La fecha de inicio no puede ser mayor a la fecha de fin");

        return await _repository.ObtenerEstadisticasAsync(fechaInicio, fechaFin, cancellationToken);
    }
}

/// <summary>
/// Interfaz para el servicio de reportes de balanza
/// </summary>
public interface IBalanzaReportService
{
    Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerarReporteExcelAsync(DateTime fechaInicio, DateTime fechaFin, string? vehiculoId = null, CancellationToken cancellationToken = default);
    Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(DateTime fechaInicio, DateTime fechaFin, CancellationToken cancellationToken = default);
}
