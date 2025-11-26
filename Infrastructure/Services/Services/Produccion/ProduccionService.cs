using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Services.Produccion;

/// <summary>
/// Servicio de aplicación para operaciones de Producción
/// Implementa la lógica de negocio y orquesta operaciones entre repositorios
/// </summary>
public class ProduccionService : IProduccionService
{
    private readonly IProduccionRepository _repository;

    public ProduccionService(IProduccionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Obtiene la lista de registros de producción con filtros
    /// </summary>
    public async Task<ApiResponse<IEnumerable<Pde>>> GetProduccion(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null)
    {
        return await _repository.GetProduccion(fechaInicio, fechaFin, materialId);
    }

    /// <summary>
    /// Obtiene el reporte PDF de un registro de producción
    /// </summary>
    public async Task<byte[]> GetReportAsync(int code)
    {
        ValidationHelper.ValidarId(code, nameof(code));
        return await _repository.GetReportAsync(code);
    }

    /// <summary>
    /// Crea o actualiza un registro de producción
    /// </summary>
    public async Task<ApiResponse<Pde>> Produccion(Pde request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await _repository.Produccion(request);
    }
}
