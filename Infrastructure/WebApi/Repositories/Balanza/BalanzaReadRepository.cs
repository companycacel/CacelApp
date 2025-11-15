using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Repositories.Login;
using WebApi.Shared;

namespace Infrastructure.WebApi.Repositories.Balanza;

/// <summary>
/// Implementación del repositorio de lectura de balanza usando API HTTP
/// Implementa el patrón Repository y se comunica con la API existente
/// </summary>
public class BalanzaReadRepository : IBalanzaReadRepository
{
    private readonly IAuthService _authService;

    public BalanzaReadRepository(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IEnumerable<Baz>> ObtenerTodosAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? vehiculoId = null,
        string? agenciaDescripcion = null,
        int? estado = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();

            var queryParams = new List<string>();
            queryParams.Add("action=G");
            if (fechaInicio.HasValue)
                queryParams.Add($"baz_fechai={fechaInicio:yyyy-MM-dd}");
            if (fechaFin.HasValue)
                queryParams.Add($"baz_fechaf={fechaFin:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(vehiculoId))
                queryParams.Add($"baz_veh_id={Uri.EscapeDataString(vehiculoId)}");
            if (!string.IsNullOrEmpty(agenciaDescripcion))
                queryParams.Add($"baz_age_des={Uri.EscapeDataString(agenciaDescripcion)}");
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

    public async Task<Baz?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var url = $"/{id}";
            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Aquí se deserializaría el JSON a BalanzaRegistro
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener el registro de balanza con ID {id}", ex);
        }
    }

    public async Task<IEnumerable<Baz>> ObtenerPorVehiculoAsync(
        string vehiculoId,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var queryParams = new List<string> { $"vehiculoId={Uri.EscapeDataString(vehiculoId)}" };
            
            if (fechaInicio.HasValue)
                queryParams.Add($"fechaInicio={fechaInicio:yyyy-MM-dd}");
            if (fechaFin.HasValue)
                queryParams.Add($"fechaFin={fechaFin:yyyy-MM-dd}");

            var queryString = "?" + string.Join("&", queryParams);
            var url = $"/recursos/veh?action=L";

            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Aquí se deserializaría el JSON a una lista de BalanzaRegistro
            return new List<Baz>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener registros del vehículo {vehiculoId}", ex);
        }
    }
}
