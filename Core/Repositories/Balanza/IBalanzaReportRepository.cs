

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

    /// <summary>
    /// Obtiene estadísticas de pesajes para un período
    /// </summary>
    Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO para estadísticas de balanza
/// </summary>
public class BalanzaEstadisticas
{
    public int TotalRegistros { get; set; }
    public decimal PesoBrutaPromedio { get; set; }
    public decimal PesoNetaPromedio { get; set; }
    public decimal MontoTotal { get; set; }
    public int TotalVehiculos { get; set; }
    public Dictionary<string, int> RegistrosPorTipo { get; set; } = new();
    public Dictionary<string, int> RegistrosPorAgencia { get; set; } = new();
}
