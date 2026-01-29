using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Produccion;

public interface IProduccionRepository
{
    /// <summary>
    /// Guarda un registro de producción (Create/Update/Delete según request.action)
    /// </summary>
    /// <param name="request">Datos del registro con action = ActionType.Create | Update | Delete</param>
    Task<ApiResponse<Pde>> SaveProduccionAsync(Pde request);
}
