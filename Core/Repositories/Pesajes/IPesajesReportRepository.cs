namespace Core.Repositories.Pesajes;

/// <summary>
/// Repositorio para generación de reportes de Pesajes
/// </summary>
public interface IPesajesReportRepository
{
    /// <summary>
    /// Genera el reporte en PDF de un pesaje
    /// </summary>
    /// <param name="id">ID del pesaje</param>
    Task<byte[]> GenerateReportPdfAsync(int id);
}
