using Core.Exceptions;
using Core.Repositories.Login;
using Core.Repositories.Profile;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using System.Net;
using System.Net.Http.Json;
using WebApi.Helper;

public class UserProfileService : IUserProfileService
{
    private readonly IAuthService _authService;

    public UserProfileService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Gus> GetUserProfileAsync()
    {  
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var response = await authenticatedClient.GetAsync("/profile");
            var result = await ResponseMap.Mapping<Gus>(response, CancellationToken.None);
            return result.Data;
    }

    public async Task<List<PermisoModulo>> GetPermisosAsync()
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var response = await authenticatedClient.GetAsync("main/gtp?gmo.gmo_type=E&action=N");

            var result = await ResponseMap.Mapping<IEnumerable<PermisoModulo>>(response, CancellationToken.None);
            return result.Data.ToList();
            
        }
        catch (WebApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WebApiException(
                message: $"Error al conectar con el servidor para obtener permisos: {ex.Message}",
                statusCode: (int)HttpStatusCode.InternalServerError
            );
        }
    }
}
