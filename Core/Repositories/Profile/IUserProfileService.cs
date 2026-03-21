using Core.Shared.Entities.Generic;

namespace Core.Repositories.Profile;

public interface IUserProfileService
{
    Task<Gus> GetUserProfileAsync();
    Task<List<PermisoModulo>> GetPermisosAsync();
}
