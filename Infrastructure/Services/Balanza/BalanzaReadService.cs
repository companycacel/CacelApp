using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Shared.Validators;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Servicio de aplicación para operaciones de lectura de balanza
/// Implementa la lógica de negocio y orquesta operaciones entre repositorios
/// </summary>
public class BalanzaReadService : IBalanzaReadService
{
    private readonly IBalanzaReadRepository _repository;

    public BalanzaReadService(IBalanzaReadRepository repository)
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

    public async Task<Baz?> ObtenerRegistroPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarId(id, nameof(id));
        return await _repository.ObtenerPorIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Baz>> ObtenerRegistrosPorVehiculoAsync(
        string vehiculoId,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarTextoNoVacio(vehiculoId, nameof(vehiculoId));
        ValidationHelper.ValidarRangoFechasOpcional(fechaInicio, fechaFin);

        return await _repository.ObtenerPorVehiculoAsync(
            vehiculoId,
            fechaInicio,
            fechaFin,
            cancellationToken);
    }
}
