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

    public async Task<UserProfileResponse> GetUserProfileAsync()
    {
        try
        {
            var authenticatedClient = _authService.GetAuthenticatedClient();
            var response = await authenticatedClient.GetAsync("/profile");

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

                throw new WebApiException(
                    message: errorJson?.message ?? "No se pudo obtener el perfil del usuario.",
                    statusCode: errorJson.statusCode,
                    errorType: errorJson?.error ?? ""
                );
            }

            var profileResponse = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

            if (profileResponse?.Data == null)
            {
                throw new WebApiException(
                    profileResponse?.Meta?.msg ?? "Error al obtener el perfil del usuario.",
                   (int)HttpStatusCode.InternalServerError
                );
            }

            return profileResponse;
        }
        catch (WebApiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WebApiException(
                message: $"Error al conectar con el servidor de perfil: {ex.Message}",
                statusCode: (int)HttpStatusCode.InternalServerError
            );
        }
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
