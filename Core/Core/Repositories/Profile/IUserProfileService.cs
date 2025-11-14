namespace Core.Repositories.Profile;

public interface IUserProfileService
{
    /// <summary>
    /// Obtiene el perfil del usuario actual desde el servidor
    /// </summary>
    /// <returns>Respuesta con datos del perfil del usuario</returns>
    Task<UserProfileResponse> GetUserProfileAsync();
}
