namespace Core.Repositories.Profile;

public interface IUserProfileService
{
    Task<UserProfileResponse> GetUserProfileAsync();
    Task<List<PermisoModulo>> GetPermisosAsync();
}
