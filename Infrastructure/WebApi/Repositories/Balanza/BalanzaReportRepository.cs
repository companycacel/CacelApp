using Core.Repositories.Balanza;

namespace Infrastructure.WebApi.Repositories.Balanza;

/// <summary>
/// Implementación del repositorio de reportes de balanza usando API HTTP
/// Maneja la generación de reportes PDF y Excel
/// </summary>
public class BalanzaReportRepository : IBalanzaReportRepository
{
    private readonly HttpClient _httpClient;
    private const string BaseEndpoint = "api/balanza/reportes";

    public BalanzaReportRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseEndpoint}/pdf/{registroId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al generar reporte PDF para el registro {registroId}", ex);
        }
    }

    public async Task<byte[]> GenerarReporteExcelAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        string? vehiculoId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"fechaInicio={fechaInicio:yyyy-MM-dd}",
                $"fechaFin={fechaFin:yyyy-MM-dd}"
            };

            if (!string.IsNullOrEmpty(vehiculoId))
                queryParams.Add($"vehiculoId={Uri.EscapeDataString(vehiculoId)}");

            var queryString = "?" + string.Join("&", queryParams);
            var url = $"{BaseEndpoint}/excel{queryString}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al generar reporte Excel", ex);
        }
    }

    public async Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryString = $"?fechaInicio={fechaInicio:yyyy-MM-dd}&fechaFin={fechaFin:yyyy-MM-dd}";
            var url = $"{BaseEndpoint}/estadisticas{queryString}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Aquí se deserializaría el JSON a BalanzaEstadisticas
            // Por ahora retornamos un objeto vacío
            return new BalanzaEstadisticas();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al obtener estadísticas de balanza", ex);
        }
    }
}
