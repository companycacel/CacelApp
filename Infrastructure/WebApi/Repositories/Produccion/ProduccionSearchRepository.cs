using Core.Repositories.Login;
using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Produccion;

/// <summary>
/// Implementación del repositorio de búsqueda de Producción usando API HTTP
/// </summary>
public class ProduccionSearchRepository : IProduccionSearchRepository
{
    private readonly IAuthService _authService;

    public ProduccionSearchRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<ApiResponse<IEnumerable<Pde>>> GetProduccionAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        var queryParams = new List<string>();
        if (fechaInicio.HasValue)
            queryParams.Add($"fechai={fechaInicio.Value:yyyy-MM-dd}");
        if (fechaFin.HasValue)
            queryParams.Add($"fechaf={fechaFin.Value:yyyy-MM-dd}");
        if (materialId.HasValue && materialId.Value > 0)
            queryParams.Add($"pde_bie_id={materialId.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var path = $"/logistica/produccion{queryString}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<IEnumerable<Pde>>(response, CancellationToken.None);
        return result;
    }
}
