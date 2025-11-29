using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;

namespace Infrastructure.Services.Pesajes;

/// <summary>
/// Servicio para operaciones de escritura (CRUD) de Pesajes
/// Utiliza ActionType en el request para determinar la operación
/// </summary>
public interface IPesajesService
{
    /// <summary>
    /// Guarda un pesaje (Create/Update/Delete según request.action)
    /// </summary>
    /// <param name="request">Entidad con action = ActionType.Create | Update | Delete</param>
    Task<ApiResponse<Pes>> SavePesajeAsync(Pes request);

    /// <summary>
    /// Guarda un detalle de pesaje (Create/Update/Delete según request.action)
    /// </summary>
    Task<ApiResponse<Pde>> SavePesajeDetalleAsync(Pde request);
}
