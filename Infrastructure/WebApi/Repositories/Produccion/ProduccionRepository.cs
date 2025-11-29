using Core.Repositories.Login;
using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Produccion;

/// <summary>
/// Implementación del repositorio CRUD de Producción usando API HTTP
/// </summary>
public class ProduccionRepository : IProduccionRepository
{
    private readonly IAuthService _authService;

    public ProduccionRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<ApiResponse<Pde>> SaveProduccionAsync(Pde request)
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
