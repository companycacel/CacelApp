namespace Core.Repositories.Produccion;

/// <summary>
/// Repositorio para generación de reportes de Producción
/// </summary>
public interface IProduccionReportRepository
{
    /// <summary>
    /// Genera el reporte en PDF de un registro de producción
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(int id);
}
