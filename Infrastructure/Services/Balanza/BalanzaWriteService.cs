using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Shared.Validators;

namespace Infrastructure.Services.Balanza;

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
        ValidationHelper.ValidarObjetoNoNulo(registro, nameof(registro));

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
        ValidationHelper.ValidarObjetoNoNulo(registro, nameof(registro));
        ValidationHelper.ValidarId(registro.baz_id, nameof(registro.baz_id));

        // Recalcular peso neto si es necesario
        registro.CalcularPesoNeto();

        // Actualizar fecha de modificación
        registro.updated = DateTime.UtcNow;

        return await _repository.ActualizarAsync(registro, cancellationToken);
    }

    public async Task<bool> EliminarRegistroAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarId(id, nameof(id));
        return await _repository.EliminarAsync(id, cancellationToken);
    }

    public async Task<bool> CambiarEstadoAsync(int id, int nuevoEstado, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarId(id, nameof(id));

        if (nuevoEstado < 0)
            throw new ArgumentException("El estado debe ser válido", nameof(nuevoEstado));

        return await _repository.CambiarEstadoAsync(id, nuevoEstado, cancellationToken);
    }
}
