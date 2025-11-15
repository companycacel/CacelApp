using Core.Repositories.Balanza;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Interfaz para el servicio de reportes de balanza
/// Define operaciones de generación de reportes y estadísticas
/// </summary>
public interface IBalanzaReportService
{
    /// <summary>
    /// Genera un reporte PDF para un registro específico
    /// </summary>
    Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera un reporte Excel con múltiples registros
    /// </summary>
    Task<byte[]> GenerarReporteExcelAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        string? vehiculoId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene estadísticas de balanza para un rango de fechas
    /// </summary>
    Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);
}
