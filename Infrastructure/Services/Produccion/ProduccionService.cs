using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Produccion;

/// <summary>
/// Servicio de aplicación para operaciones de escritura (CRUD) de Producción
/// Implementa la lógica de negocio y validaciones para crear/actualizar/eliminar registros
/// Utiliza ActionType en el request para determinar la operación
/// </summary>
public class ProduccionService : Infrastructure.Services.Produccion.IProduccionService
{
    private readonly IProduccionRepository _repository;

    public ProduccionService(IProduccionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Guarda un registro de producción (Create/Update/Delete según request.action)
    /// </summary>
    public async Task<ApiResponse<Pde>> SaveProduccionAsync(Pde request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Validar según el tipo de acción
        if (request.action == ActionType.Update || request.action == ActionType.Delete)
        {
            ValidationHelper.ValidarId(request.pde_id, nameof(request.pde_id));
        }

        return await _repository.SaveProduccionAsync(request);
    }
}
