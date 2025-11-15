using Core.Repositories.Balanza;
using Core.Repositories.Login;

namespace Infrastructure.WebApi.Repositories.Balanza;

/// <summary>
/// Implementación del repositorio de reportes de balanza usando API HTTP
/// Maneja la generación de reportes PDF y Excel
/// </summary>
public class BalanzaReportRepository : IBalanzaReportRepository
{
    private readonly IAuthService _authService;

    public BalanzaReportRepository(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var url = $"/logistica/balanza/{registroId}";
            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al generar reporte PDF para el registro {registroId}", ex);
        }
    }



    public async Task<BalanzaEstadisticas> ObtenerEstadisticasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var queryString = $"?fechaInicio={fechaInicio:yyyy-MM-dd}&fechaFin={fechaFin:yyyy-MM-dd}";
            var url = $"/estadisticas{queryString}";

            var response = await authenticatedClient.GetAsync(url, cancellationToken);
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
