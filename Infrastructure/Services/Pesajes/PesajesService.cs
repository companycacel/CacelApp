using Core.Repositories.Pesajes;
using Core.Repositories.Pesajes.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Pesajes;

/// <summary>
/// Servicio de aplicación para operaciones de escritura (CRUD) de Pesajes
/// Implementa la lógica de negocio y validaciones para crear/actualizar/eliminar registros
/// Utiliza ActionType en el request para determinar la operación
/// </summary>
public class PesajesService : Infrastructure.Services.Pesajes.IPesajesService
{
    private readonly IPesajesRepository _repository;

    public PesajesService(IPesajesRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Guarda un pesaje (Create/Update/Delete según request.action)
    /// </summary>
    public async Task<ApiResponse<Pes>> SavePesajeAsync(Pes request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validar según el tipo de acción
        if (request.action == ActionType.Update || request.action == ActionType.Delete)
        {
            ValidationHelper.ValidarId(request.pes_id, nameof(request.pes_id));
        }

        return await _repository.SavePesajeAsync(request);
    }

    /// <summary>
    /// Guarda un detalle de pesaje (Create/Update/Delete según request.action)
    /// </summary>
    public async Task<ApiResponse<Pde>> SavePesajeDetalleAsync(Pde request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _repository.SavePesajeDetalleAsync(request);
    }
}
