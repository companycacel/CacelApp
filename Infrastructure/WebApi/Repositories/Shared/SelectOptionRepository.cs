using Core.Repositories.Login;
using Core.Repositories.Shared;
using Core.Shared.Entities;
using Core.Shared.Enums;
using System.Text.Json;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Shared;

/// <summary>
/// Implementación del repositorio de SelectOptions usando API HTTP
/// Se conecta con los endpoints de recursos de la API
/// </summary>
public class SelectOptionRepository : ISelectOptionRepository
{
    private readonly IAuthService _authService;

    public SelectOptionRepository(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IEnumerable<SelectOption>> GetSelectOptionsAsync(
        SelectOptionType type,
        int? code = null,
        object? additionalParams = null,
        CancellationToken cancellationToken = default)
    {
        return type switch
        {
            SelectOptionType.TipoPago => await GetTipoPagoAsync(cancellationToken),
            SelectOptionType.Colaborador => await GetColaboradorAsync(code, cancellationToken),
            SelectOptionType.Material => await GetMaterialAsync(code, additionalParams, cancellationToken),
            SelectOptionType.Umedida => await GetUmedidaAsync(cancellationToken),
            _ => throw new ArgumentException($"Tipo de lista no válido: {type}", nameof(type))
        };
    }

    /// <summary>
    /// Obtiene la lista de tipos de pago
    /// </summary>
    private async Task<IEnumerable<SelectOption>> GetTipoPagoAsync(CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var url = $"/recursos/t1m?action=S&t1m_status=3";
            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<SelectOption>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data ?? Enumerable.Empty<SelectOption>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al obtener tipos de pago", ex);
        }
    }

    /// <summary>
    /// Obtiene la lista de colaboradores filtrados por puesto
    /// </summary>
    private async Task<IEnumerable<SelectOption>> GetColaboradorAsync(int? code, CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var url = $"/recursos/col?action=S&col_pus_id={code}";
            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<SelectOption>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data ?? Enumerable.Empty<SelectOption>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener colaboradores para el puesto {code}", ex);
        }
    }

    /// <summary>
    /// Obtiene la lista de materiales/bienes
    /// </summary>
    private async Task<IEnumerable<SelectOption>> GetMaterialAsync(int? code,object? additionalParams, CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var qs = BuildQueryParams.Make(additionalParams);
            var url = $"/recursos/bie?action=S{(string.IsNullOrEmpty(qs) ? "" : "&" + qs)}";
            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<SelectOption>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data ?? Enumerable.Empty<SelectOption>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al obtener materiales", ex);
        }
    }

    /// <summary>
    /// Obtiene la lista de unidades de medida
    /// </summary>
    private async Task<IEnumerable<SelectOption>> GetUmedidaAsync(CancellationToken cancellationToken)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var url = $"/recursos/t6m?action=S&t6m_status=3";
            var response = await authenticatedClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<SelectOption>>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data ?? Enumerable.Empty<SelectOption>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al obtener unidades de medida", ex);
        }
    }

    /// <summary>
    /// Clase auxiliar para deserializar la respuesta de la API
    /// </summary>
    private class ApiResponse<T>
    {
        public T? Data { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
}
