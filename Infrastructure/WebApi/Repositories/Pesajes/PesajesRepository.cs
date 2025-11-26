using Core.Repositories.Pesajes;
using Core.Repositories.Login;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using System.Text.Json;
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
        
        try
        {
            var path = $"/logistica/pesajes?pes_tipo={pes_tipo}";
            var response = await authenticatedClient.GetAsync(path);

            var result = await ResponseMap.Mapping<IEnumerable<Pes>>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener pesajes tipo {pes_tipo}", ex);
        }
    }

    /// <summary>
    /// Obtiene un pesaje por su ID
    /// </summary>
    public async Task<ApiResponse<Pes>> GetPesajesById(int id)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var path = $"/logistica/pesajes?action=I&pes_id={id}";
            var response = await authenticatedClient.GetAsync(path);

            var result = await ResponseMap.Mapping<Pes>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener pesaje con ID {id}", ex);
        }
    }

    /// <summary>
    /// Obtiene el reporte en PDF de un pesaje
    /// </summary>
    public async Task<byte[]> GetReportAsync(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var path = $"/logistica/pesajes/{code}";
            var response = await authenticatedClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return Array.Empty<byte>();
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Crea o actualiza un pesaje
    /// </summary>
    public async Task<ApiResponse<Pes>> Pesajes(Pes request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var path = $"/logistica/pesajes";
            var response = await authenticatedClient.PostAsync(path, content);

            var result = await ResponseMap.Mapping<Pes>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al procesar pesaje", ex);
        }
    }

    /// <summary>
    /// Obtiene el detalle de pesajes (pde) para un pesaje específico
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetPesajesDetalle(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var path = $"/logistica/pesajes/detalle?pes_id={code}";
            var response = await authenticatedClient.GetAsync(path);

            var result = await ResponseMap.Mapping<IEnumerable<Pde>>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener detalle de pesaje {code}", ex);
        }
    }

    /// <summary>
    /// Crea o actualiza un detalle de pesaje
    /// </summary>
    public async Task<ApiResponse<Pde>> PesajesDetalle(Pde request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            HttpResponseMessage response;

            if (request.files != null && request.files.Any())
            {
                using var content = new MultipartFormDataContent();
                
                // Agregar propiedades del objeto como StringContent
                // Serializamos el objeto sin los archivos para enviarlo como parte del form-data o como campos individuales
                // En este caso, asumiremos que la API espera los campos individuales o un campo 'json'
                // Para simplificar y mantener compatibilidad, enviaremos los campos clave
                
                content.Add(new StringContent(request.pde_id.ToString()), nameof(request.pde_id));
                content.Add(new StringContent(request.pde_pes_id.ToString()), nameof(request.pde_pes_id));
                if (request.pde_mde_id.HasValue) content.Add(new StringContent(request.pde_mde_id.Value.ToString()), nameof(request.pde_mde_id));
                content.Add(new StringContent(request.pde_bie_id.ToString()), nameof(request.pde_bie_id));
                content.Add(new StringContent(request.pde_nbza ?? ""), nameof(request.pde_nbza));
                content.Add(new StringContent(request.pde_pb.ToString()), nameof(request.pde_pb));
                content.Add(new StringContent(request.pde_pt.ToString()), nameof(request.pde_pt));
                content.Add(new StringContent(request.pde_pn.ToString()), nameof(request.pde_pn));
                content.Add(new StringContent(request.pde_obs ?? ""), nameof(request.pde_obs));
                content.Add(new StringContent(request.pde_tipo.ToString()), nameof(request.pde_tipo));
                if (request.pde_t6m_id.HasValue) content.Add(new StringContent(request.pde_t6m_id.Value.ToString()), nameof(request.pde_t6m_id));
                content.Add(new StringContent(request.action ?? "Create"), nameof(request.action));

                // Agregar archivos
                foreach (var file in request.files)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    content.Add(fileContent, "files", file.FileName);
                }

                var path = $"/logistica/pesajes/detalle";
                response = await authenticatedClient.PostAsync(path, content);
            }
            else
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var path = $"/logistica/pesajes/detalle";
                response = await authenticatedClient.PostAsync(path, content);
            }

            var result = await ResponseMap.Mapping<Pde>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al procesar detalle de pesaje", ex);
        }
    }
}
