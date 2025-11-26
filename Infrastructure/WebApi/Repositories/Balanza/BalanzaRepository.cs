using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Repositories.Login;
using Core.Shared.Entities.Generic;
using WebApi.Helper;

namespace Infrastructure.WebApi.Repositories.Balanza;

/// <summary>
/// Implementación del repositorio de escritura de balanza usando API HTTP
/// Implementa el patrón Repository para operaciones CRUD
/// </summary>
public class BalanzaRepository : IBalanzaRepository
{
    private readonly IAuthService _authService;

    public BalanzaRepository(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public async Task<Baz> Balanza(Baz request, CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            request.baz_col_id = null;
            request.veh_veh_neje = request.veh.veh_neje;
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

            var response = await authenticatedClient.PostAsync("/logistica/balanza", form, cancellationToken);
            var result = await ResponseMap.Mapping<Baz>(response, CancellationToken.None);
            return result.Data;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al crear el registro de balanza", ex);
        }
    }
}
