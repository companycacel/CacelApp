using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Shared.Validators;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Servicio de aplicación para operaciones de lectura de balanza
/// Implementa la lógica de negocio y orquesta operaciones entre repositorios
/// </summary>
public class BalanzaSearchService : IBalanzaSearchService
{
    private readonly IBalanzaSearchRepository _repository;

    public BalanzaSearchService(IBalanzaSearchRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<Baz>> ObtenerRegistrosAsync(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? vehiculoId,
        string? Agente,
        int? estado,
        CancellationToken cancellationToken = default)
    {
        // Validar rango de fechas usando helper centralizado
        ValidationHelper.ValidarRangoFechasOpcional(fechaInicio, fechaFin);

        return await _repository.ObtenerTodosAsync(
            fechaInicio,
            fechaFin,
            vehiculoId,
            Agente,
            estado,
            cancellationToken);
    }
}
