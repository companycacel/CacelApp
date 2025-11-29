using Core.Repositories.Login;
using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Produccion;

/// <summary>
/// Implementación del repositorio de Producción usando API HTTP
/// Se comunica con la API REST para operaciones de producción
/// </summary>
public class ProduccionRepository : IProduccionRepository
{
    private readonly IAuthService _authService;

    public ProduccionRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Obtiene el listado de registros de producción con filtros
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetProduccion(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        // Construir query string con los filtros
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

    /// <summary>
    /// Obtiene el reporte en PDF de un registro de producción
    /// </summary>
    public async Task<byte[]> GetReportAsync(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();


        var path = $"/logistica/produccion/{code}";
        var response = await authenticatedClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        return Array.Empty<byte>();

    }

    /// <summary>
    /// Crea o actualiza un registro de producción
    /// </summary>
    public async Task<ApiResponse<Pde>> Produccion(Pde request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();


        using var form = new MultipartFormDataContent();


        // Campos simples
        var props = request.GetType().GetProperties();
        foreach (var prop in props)
        {
            var val = prop.GetValue(request)?.ToString() ?? "";
            form.Add(new StringContent(val), prop.Name);
        }
        // Archivos
        if (request.files != null)
        {
            foreach (var file in request.files)
            {
                var stream = file.OpenReadStream();
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                form.Add(fileContent, "files", file.FileName);
            }
        }
        var response = await authenticatedClient.PostAsync("/logistica/produccion", form);

        var result = await ResponseMap.Mapping<Pde>(response, CancellationToken.None);
        return result;

    }
}
