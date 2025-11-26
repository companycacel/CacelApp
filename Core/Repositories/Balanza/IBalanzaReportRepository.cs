

namespace Core.Repositories.Balanza;

/// <summary>
/// Interfaz para generar reportes de balanza
/// Separación de responsabilidades para operaciones de reporte
/// </summary>
public interface IBalanzaReportRepository
{
    /// <summary>
    /// Genera un reporte en formato PDF para un registro de balanza
    /// </summary>
    Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default);

}