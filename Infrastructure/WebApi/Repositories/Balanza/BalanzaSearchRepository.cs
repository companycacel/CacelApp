using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Repositories.Login;
using System.Web;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Balanza;

/// <summary>
/// Implementación del repositorio de lectura de balanza usando API HTTP
/// Implementa el patrón Repository y se comunica con la API existente
/// </summary>
public class BalanzaSearchRepository : IBalanzaSearchRepository
{
    private readonly IAuthService _authService;

    public BalanzaSearchRepository(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IEnumerable<Baz>> ObtenerTodosAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? vehiculoId = null,
        string? Agente = null,
        int? estado = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();

            var queryParams = new List<string>();
            queryParams.Add("action=G");
            if (fechaInicio.HasValue)
                queryParams.Add($"baz_fechai={HttpUtility.UrlEncode(fechaInicio?.ToString("o"))}");
            if (fechaFin.HasValue)
                queryParams.Add($"baz_fechaf={HttpUtility.UrlEncode(fechaFin?.ToString("o"))}");
            if (!string.IsNullOrEmpty(vehiculoId))
                queryParams.Add($"baz_veh_id={Uri.EscapeDataString(vehiculoId)}");
            if (!string.IsNullOrEmpty(Agente))
                queryParams.Add($"baz_age_des={Uri.EscapeDataString(Agente)}");
            if (estado.HasValue)
                queryParams.Add($"baz_status={estado}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"/logistica/balanza{queryString}";

            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await ResponseMap.Mapping<IEnumerable<Baz>>(response,cancellationToken);
            return result.Data;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al obtener registros de balanza", ex);
        }
    }
}
