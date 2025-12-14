using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Core.Repositories.Pesajes;

/// <summary>
/// Repositorio para operaciones CRUD de Pesajes
/// Utiliza ActionType en el request para determinar la operación
/// </summary>
public interface IPesajesRepository
{
    /// <summary>
    /// Guarda un pesaje (Create/Update/Delete según request.action)
    /// </summary>
    /// <param name="request">Datos del pesaje con action = ActionType.Create | Update | Delete</param>
    Task<ApiResponse<Pes>> SavePesajeAsync(Pes request);

    /// <summary>
    /// Guarda un detalle de pesaje (Create/Update/Delete según request.action)
    /// </summary>
    /// <param name="request">Datos del detalle con action = ActionType.Create | Update | Delete</param>
    Task<ApiResponse<Pde>> SavePesajeDetalleAsync(Pde request);
}
