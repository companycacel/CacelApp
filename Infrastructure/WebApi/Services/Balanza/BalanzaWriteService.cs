using Core.Domain.Balanza.Entities;
using Core.Repositories.Balanza;

namespace Infrastructure.WebApi.Services.Balanza;

/// <summary>
/// Servicio de aplicación para operaciones de escritura de balanza
/// Implementa la lógica de negocio y validaciones para crear/actualizar registros
/// </summary>
public class BalanzaWriteService : IBalanzaWriteService
{
    private readonly IBalanzaWriteRepository _repository;

    public BalanzaWriteService(IBalanzaWriteRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Baz> CrearRegistroAsync(Baz registro, CancellationToken cancellationToken = default)
    {
        if (registro == null)
            throw new ArgumentNullException(nameof(registro));

        // Validar que el registro sea válido
        if (!registro.EsValido())
            throw new InvalidOperationException("El registro de balanza no es válido");

        // Calcular peso neto si es necesario
        registro.CalcularPesoNeto();

        // Asignar fechas
        registro.created = DateTime.UtcNow;
        registro.updated = DateTime.UtcNow;

        return await _repository.CrearAsync(registro, cancellationToken);
    }

    public async Task<Baz> ActualizarRegistroAsync(Baz registro, CancellationToken cancellationToken = default)
    {
        if (registro == null)
            throw new ArgumentNullException(nameof(registro));

        if (registro.baz_id <= 0)
            throw new ArgumentException("El ID del registro debe ser válido", nameof(registro));

        // Recalcular peso neto si es necesario
        registro.CalcularPesoNeto();

        // Actualizar fecha de modificación
        registro.updated = DateTime.UtcNow;

        return await _repository.ActualizarAsync(registro, cancellationToken);
    }

    public async Task<bool> EliminarRegistroAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("El ID debe ser mayor a 0", nameof(id));

        return await _repository.EliminarAsync(id, cancellationToken);
    }

    public async Task<bool> CambiarEstadoAsync(int id, int nuevoEstado, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("El ID debe ser mayor a 0", nameof(id));

        if (nuevoEstado < 0)
            throw new ArgumentException("El estado debe ser válido", nameof(nuevoEstado));

        return await _repository.CambiarEstadoAsync(id, nuevoEstado, cancellationToken);
    }
}

/// <summary>
/// Interfaz para el servicio de escritura de balanza
/// </summary>
public interface IBalanzaWriteService
{
    Task<Baz> CrearRegistroAsync(Baz registro, CancellationToken cancellationToken = default);
    Task<Baz> ActualizarRegistroAsync(Baz registro, CancellationToken cancellationToken = default);
    Task<bool> EliminarRegistroAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CambiarEstadoAsync(int id, int nuevoEstado, CancellationToken cancellationToken = default);
}
