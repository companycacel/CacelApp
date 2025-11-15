using Core.Repositories.Balanza.Entities;

namespace Core.Repositories.Balanza;

/// <summary>
/// Interfaz que define el contrato para operaciones de escritura de registros de balanza
/// Implementa el patrón Repository y separación de responsabilidades (CQRS)
/// </summary>
public interface IBalanzaWriteRepository
{
    /// <summary>
    /// Crea un nuevo registro de balanza
    /// </summary>
    Task<Baz> CrearAsync(Baz registro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un registro de balanza existente
    /// </summary>
    Task<Baz> ActualizarAsync(Baz registro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un registro de balanza por su ID
    /// </summary>
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cambia el estado de un registro de balanza
    /// </summary>
    Task<bool> CambiarEstadoAsync(int id, int nuevoEstado, CancellationToken cancellationToken = default);
}
