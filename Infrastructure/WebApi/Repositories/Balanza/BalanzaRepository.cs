using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Repositories.Login;
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

        var authenticatedClient = _authService.GetAuthenticatedClient();
        using var form = new MultipartFormDataContent();

        var props = request.GetType().GetProperties();
        foreach (var prop in props)
        {
            if (prop.Name == "files") continue;

            var value = prop.GetValue(request);
            if (value == null) continue;

            if (prop.PropertyType.Namespace == "Core.Shared.Entities" ||
                prop.PropertyType.Namespace == "Core.Shared.Entities.Generic")
            {
                var subProps = value.GetType().GetProperties();
                foreach (var subProp in subProps)
                {
                    var subVal = subProp.GetValue(value)?.ToString() ?? "";
                    form.Add(new StringContent(subVal), $"{prop.Name}.{subProp.Name}");
                }
            }
            else
            {
                form.Add(new StringContent(value.ToString() ?? ""), prop.Name);
            }
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

        var response = await authenticatedClient.PostAsync("/logistica/balanza", form, cancellationToken);
        var result = await ResponseMap.Mapping<Baz>(response, CancellationToken.None);
        return result.Data;
    }
}
