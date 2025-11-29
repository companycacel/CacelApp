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

        var authenticatedClient = _authService.GetAuthenticatedClient();
        var url = $"/logistica/balanza/{registroId}";
        var response = await authenticatedClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);

    }
}
