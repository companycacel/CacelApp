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
}
