using Core.Repositories.Login;
using Core.Repositories.Pesajes;
using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Pesajes;

/// <summary>
/// Implementación del repositorio de búsqueda de Pesajes usando API HTTP
/// </summary>
public class PesajesSearchRepository : IPesajesSearchRepository
{
    private readonly IAuthService _authService;

    public PesajesSearchRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<ApiResponse<IEnumerable<Pes>>> GetPesajesAsync(string tipo)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var path = $"/logistica/pesajes?pes_tipo={tipo}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<IEnumerable<Pes>>(response, CancellationToken.None);
        return result;
    }

    public async Task<ApiResponse<Pes>> GetPesajeByIdAsync(int id)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var path = $"/logistica/pesajes?action=I&pes_id={id}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<Pes>(response, CancellationToken.None);
        return result;
    }

    public async Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalleAsync(int pesajeId)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var path = $"/logistica/pdetalles?action=G&pde_pes_id={pesajeId}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<IEnumerable<Pde>>(response, CancellationToken.None);
        return result;
    }

    public async Task<ApiResponse<IEnumerable<DocumentoPes>>> GetDocumentosAsync()
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var response = authenticatedClient.GetAsync("/comercial/mdetalles?action=L&mde_dev=1").Result;

        var result = await ResponseMap.Mapping<IEnumerable<DocumentoPes>>(response, CancellationToken.None);
        return result;
    }
}
