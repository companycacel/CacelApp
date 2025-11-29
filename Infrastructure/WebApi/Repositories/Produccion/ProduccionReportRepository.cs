using Core.Repositories.Login;
using Core.Repositories.Produccion;

namespace Infrastructure.WebApi.Repositories.Produccion;

/// <summary>
/// Implementación del repositorio de reportes de Producción usando API HTTP
/// </summary>
public class ProduccionReportRepository : IProduccionReportRepository
{
    private readonly IAuthService _authService;

    public ProduccionReportRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<byte[]> GenerateReportPdfAsync(int id)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var path = $"/logistica/produccion/{id}";
        var response = await authenticatedClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        return Array.Empty<byte>();
    }
}
