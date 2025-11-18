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
        
        try
        {
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
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al obtener registros de producción", ex);
        }
    }

    /// <summary>
    /// Obtiene un registro de producción por su ID
    /// </summary>
    public async Task<ApiResponse<Pde>> GetProduccionById(int id)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var path = $"/logistica/produccion?action=I&pde_id={id}";
            var response = await authenticatedClient.GetAsync(path);

            var result = await ResponseMap.Mapping<Pde>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener producción con ID {id}", ex);
        }
    }

    /// <summary>
    /// Obtiene el reporte en PDF de un registro de producción
    /// </summary>
    public async Task<byte[]> GetReportAsync(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var path = $"/logistica/produccion/{code}";
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
    /// Crea o actualiza un registro de producción
    /// </summary>
    public async Task<ApiResponse<Pde>> Produccion(Pde request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var content = new MultipartFormDataContent();
            
            // Agregar datos del registro
            content.Add(new StringContent(request.pde_id.ToString()), "pde_id");
            content.Add(new StringContent(request.pde_pes_id.ToString()), "pde_pes_id");
            content.Add(new StringContent(request.pde_bie_id.ToString()), "pde_bie_id");
            content.Add(new StringContent(request.pde_pb.ToString()), "pde_pb");
            content.Add(new StringContent(request.pde_pt.ToString()), "pde_pt");
            content.Add(new StringContent(request.pde_pn.ToString()), "pde_pn");
            
            if (!string.IsNullOrEmpty(request.pde_nbza))
                content.Add(new StringContent(request.pde_nbza), "pde_nbza");
                
            if (!string.IsNullOrEmpty(request.pde_obs))
                content.Add(new StringContent(request.pde_obs), "pde_obs");
            
            if (!string.IsNullOrEmpty(request.action))
                content.Add(new StringContent(request.action), "action");

            // Agregar archivos si existen
            if (request.files != null && request.files.Any())
            {
                foreach (var file in request.files)
                {
                    var stream = file.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    content.Add(fileContent, "files", file.FileName);
                }
            }

            var path = "/logistica/produccion";
            var response = await authenticatedClient.PostAsync(path, content);

            var result = await ResponseMap.Mapping<Pde>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al procesar producción", ex);
        }
    }

    /// <summary>
    /// Obtiene el detalle de producción para un pesaje específico
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetProduccionDetalle(int code)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var path = $"/logistica/produccion/detalle?pes_id={code}";
            var response = await authenticatedClient.GetAsync(path);

            var result = await ResponseMap.Mapping<IEnumerable<Pde>>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener detalle de producción {code}", ex);
        }
    }

    /// <summary>
    /// Crea o actualiza un detalle de producción
    /// </summary>
    public async Task<ApiResponse<Pde>> ProduccionDetalle(Pde request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        
        try
        {
            var content = new MultipartFormDataContent();
            
            // Agregar datos del detalle
            content.Add(new StringContent(request.pde_id.ToString()), "pde_id");
            content.Add(new StringContent(request.pde_pes_id.ToString()), "pde_pes_id");
            content.Add(new StringContent(request.pde_bie_id.ToString()), "pde_bie_id");
            content.Add(new StringContent(request.pde_pb.ToString()), "pde_pb");
            content.Add(new StringContent(request.pde_pt.ToString()), "pde_pt");
            content.Add(new StringContent(request.pde_pn.ToString()), "pde_pn");
            
            if (!string.IsNullOrEmpty(request.pde_nbza))
                content.Add(new StringContent(request.pde_nbza), "pde_nbza");
                
            if (!string.IsNullOrEmpty(request.pde_obs))
                content.Add(new StringContent(request.pde_obs), "pde_obs");
            
            if (!string.IsNullOrEmpty(request.action))
                content.Add(new StringContent(request.action), "action");

            // Agregar archivos si existen
            if (request.files != null && request.files.Any())
            {
                foreach (var file in request.files)
                {
                    var stream = file.OpenReadStream();
                    var fileContent = new StreamContent(stream);
                    content.Add(fileContent, "files", file.FileName);
                }
            }

            var path = "/logistica/produccion/detalle";
            var response = await authenticatedClient.PostAsync(path, content);

            var result = await ResponseMap.Mapping<Pde>(response, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al procesar detalle de producción", ex);
        }
    }
}
