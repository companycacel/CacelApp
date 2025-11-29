using Core.Repositories.Login;
using Core.Repositories.Pesajes;

namespace Infrastructure.WebApi.Repositories.Pesajes;

/// <summary>
/// Implementación del repositorio de reportes de Pesajes usando API HTTP
/// </summary>
public class PesajesReportRepository : IPesajesReportRepository
{
    private readonly IAuthService _authService;

    public PesajesReportRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<byte[]> GenerateReportPdfAsync(int id)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var path = $"/logistica/pesajes/{id}";
        var response = await authenticatedClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        return Array.Empty<byte>();
    }
}
