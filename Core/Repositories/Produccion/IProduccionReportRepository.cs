namespace Core.Repositories.Produccion;

public interface IProduccionReportRepository
{
    /// <summary>
    /// Genera el reporte en PDF de un registro de producción
    /// </summary>
    Task<byte[]> GenerateReportPdfAsync(int id);
}
