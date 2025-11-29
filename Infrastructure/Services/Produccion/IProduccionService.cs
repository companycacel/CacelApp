using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Infrastructure.Services.Produccion;

/// <summary>
/// Servicio para operaciones de escritura (CRUD) de Producción
/// Utiliza ActionType en el request para determinar la operación
/// </summary>
public interface IProduccionService
{
    /// <summary>
    /// Guarda un registro de producción (Create/Update/Delete según request.action)
    /// </summary>
    /// <param name="request">Entidad con action = ActionType.Create | Update | Delete</param>
    Task<ApiResponse<Pde>> SaveProduccionAsync(Pde request);
}
