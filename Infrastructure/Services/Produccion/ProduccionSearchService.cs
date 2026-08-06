using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Validators;

namespace Infrastructure.Services.Produccion;

/// <summary>
/// Servicio de aplicación para operaciones de lectura y búsqueda de Producción
/// </summary>
public class ProduccionSearchService : IProduccionSearchService
{
    private readonly IProduccionSearchRepository _searchRepository;

    public ProduccionSearchService(IProduccionSearchRepository searchRepository)
    {
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
    }
    public async Task<ApiResponse<IEnumerable<Pde>>> SearchProduccionAsync(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? materialId = null)
    {
        return await _searchRepository.GetProduccionAsync(fechaInicio, fechaFin, materialId);
    }
}
