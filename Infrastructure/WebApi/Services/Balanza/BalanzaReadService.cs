using Core.Domain.Balanza.Entities;
using Core.Repositories.Balanza;

namespace Infrastructure.WebApi.Services.Balanza;

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

    public async Task<IEnumerable<Core.Domain.Balanza.Entities.Baz>> ObtenerRegistrosAsync(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? vehiculoId,
        string? agenciaDescripcion,
        int? estado,
        CancellationToken cancellationToken = default)
    {
        // Validar parámetros
        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
        {
            throw new InvalidOperationException("La fecha de inicio no puede ser mayor a la fecha de fin");
        }

        return await _repository.ObtenerTodosAsync(
            fechaInicio,
            fechaFin,
            vehiculoId,
            agenciaDescripcion,
            estado,
            cancellationToken);
    }

    public async Task<Core.Domain.Balanza.Entities.Baz?> ObtenerRegistroPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("El ID debe ser mayor a 0", nameof(id));

        return await _repository.ObtenerPorIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Core.Domain.Balanza.Entities.Baz>> ObtenerRegistrosPorVehiculoAsync(
        string vehiculoId,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vehiculoId))
            throw new ArgumentException("El ID del vehículo es requerido", nameof(vehiculoId));

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio > fechaFin)
            throw new InvalidOperationException("La fecha de inicio no puede ser mayor a la fecha de fin");

        return await _repository.ObtenerPorVehiculoAsync(
            vehiculoId,
            fechaInicio,
            fechaFin,
            cancellationToken);
    }
}

/// <summary>
/// Interfaz para el servicio de lectura de balanza
/// </summary>
public interface IBalanzaReadService
{
    Task<IEnumerable<Core.Domain.Balanza.Entities.Baz>> ObtenerRegistrosAsync(
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? vehiculoId,
        string? agenciaDescripcion,
        int? estado,
        CancellationToken cancellationToken = default);

    Task<Core.Domain.Balanza.Entities.Baz?> ObtenerRegistroPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Core.Domain.Balanza.Entities.Baz>> ObtenerRegistrosPorVehiculoAsync(
        string vehiculoId,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        CancellationToken cancellationToken = default);
}
