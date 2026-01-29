namespace Core.Repositories.Pesajes;

/// <summary>
/// Repositorio para generación de reportes de Pesajes
/// </summary>
public interface IPesajesReportRepository
{
    Task<byte[]> GenerateReportPdfAsync(int id);
}
