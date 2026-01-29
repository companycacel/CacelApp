namespace Core.Repositories.Profile;

public interface IUserProfileService
{
    /// <summary>
    /// Obtiene el perfil del usuario actual desde el servidor
    /// </summary>
    /// <returns>Respuesta con datos del perfil del usuario</returns>
    Task<UserProfileResponse> GetUserProfileAsync();

    /// <summary>
    /// Obtiene los permisos de los módulos del usuario actual
    /// </summary>
    /// <returns>Lista de permisos por módulo</returns>
    Task<List<PermisoModulo>> GetPermisosAsync();
}
