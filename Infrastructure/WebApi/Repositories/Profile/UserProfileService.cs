using Core.Exceptions;
using Core.Repositories.Login;
using Core.Repositories.Profile;
using Core.Shared.Entities;
using System.Net;
using System.Net.Http.Json;

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
            var response = await authenticatedClient.GetAsync("profile");

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

                throw new WebApiException(
                    message: errorJson?.message ?? "No se pudo obtener el perfil del usuario.",
                    statusCode: response.StatusCode,
                    errorType: errorJson?.error ?? ""
                );
            }

            var profileResponse = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

            if (profileResponse?.Data == null)
            {
                throw new WebApiException(
                    profileResponse?.Meta?.msg ?? "Error al obtener el perfil del usuario.",
                    HttpStatusCode.InternalServerError
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
                statusCode: HttpStatusCode.InternalServerError
            );
        }
    }
}
