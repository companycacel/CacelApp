using Core.Repositories.Login;
using Core.Repositories.Pesajes;
using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using System.Net.Http.Json;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Pesajes;

/// <summary>
/// Implementación del repositorio de Pesajes usando API HTTP
/// Se comunica con la API REST para operaciones de pesajes
/// </summary>
public class PesajesRepository : IPesajesRepository
{
    private readonly IAuthService _authService;

    public PesajesRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Obtiene el listado de pesajes filtrado por tipo
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pes>>> GetPesajes(string pes_tipo)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var path = $"/logistica/pesajes?pes_tipo={pes_tipo}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<IEnumerable<Pes>>(response, CancellationToken.None);
        return result;
    }

    /// <summary>
    /// Obtiene un pesaje por su ID
    /// </summary>
    public async Task<ApiResponse<Pes>> GetPesajesById(int id)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();

        var path = $"/logistica/pesajes?action=I&pes_id={id}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<Pes>(response, CancellationToken.None);
        return result;

    }

    /// <summary>
    /// Obtiene el reporte en PDF de un pesaje
    /// </summary>
    public async Task<byte[]> GetReportAsync(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();

        var path = $"/logistica/pesajes/{code}";
        var response = await authenticatedClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        return Array.Empty<byte>();

    }

    /// <summary>
    /// Crea o actualiza un pesaje
    /// </summary>
    public async Task<ApiResponse<Pes>> Pesajes(Pes request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();

        var response = authenticatedClient.PostAsJsonAsync("/logistica/pesajes", request).Result;

        var result = await ResponseMap.Mapping<Pes>(response, CancellationToken.None);
        return result;

    }


    /// <summary>
    /// Crea o actualiza un detalle de pesaje
    /// </summary>
    public async Task<ApiResponse<Pde>> PesajesDetalle(Pde request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();

        using var form = new MultipartFormDataContent();
        var props = request.GetType().GetProperties();
        foreach (var prop in props)
        {
            var val = prop.GetValue(request)?.ToString() ?? "";
            form.Add(new StringContent(val), prop.Name);
        }
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
        var response = authenticatedClient.PostAsync("/logistica/pdetalles", form).Result;

        var result = await ResponseMap.Mapping<Pde>(response, CancellationToken.None);
        return result;

    }
    /// <summary>
    /// Obtiene el detalle de pesajes (pde) para un pesaje específico
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalle(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();

        var path = $"/logistica/pdetalles?action=G&pde_pes_id={code}";
        var response = await authenticatedClient.GetAsync(path);

        var result = await ResponseMap.Mapping<IEnumerable<Pde>>(response, CancellationToken.None);
        return result;

    }


    /// <summary>
    /// Agregar listado de Documentos a un Pesaje
    /// </summary>
    public async Task<ApiResponse<IEnumerable<DocumentoPes>>> document()
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();

        var response = authenticatedClient.GetAsync("/comercial/mdetalles?action=L&mde_dev=1").Result;

        var result = await ResponseMap.Mapping<IEnumerable<DocumentoPes>>(response, CancellationToken.None);
        return result;

    }

}
