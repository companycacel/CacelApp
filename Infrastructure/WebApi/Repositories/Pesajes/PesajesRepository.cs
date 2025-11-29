using Core.Repositories.Login;
using Core.Repositories.Pesajes;
using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using System.Net.Http.Json;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Pesajes;

/// <summary>
/// Implementación del repositorio CRUD de Pesajes usando API HTTP
/// </summary>
public class PesajesRepository : IPesajesRepository
{
    private readonly IAuthService _authService;

    public PesajesRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<ApiResponse<Pes>> SavePesajeAsync(Pes request)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var response = authenticatedClient.PostAsJsonAsync("/logistica/pesajes", request).Result;

        var result = await ResponseMap.Mapping<Pes>(response, CancellationToken.None);
        return result;
    }

    public async Task<ApiResponse<Pde>> SavePesajeDetalleAsync(Pde request)
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
}
