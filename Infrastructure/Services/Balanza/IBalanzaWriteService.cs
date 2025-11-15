using Core.Repositories.Balanza.Entities;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Interfaz para el servicio de escritura de balanza
/// Define operaciones de creación, actualización y eliminación
/// </summary>
public interface IBalanzaWriteService
{
    /// <summary>
    /// Crea un nuevo registro de balanza
    /// </summary>
    Task<Baz> CrearRegistroAsync(Baz registro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un registro de balanza existente
    /// </summary>
    Task<Baz> ActualizarRegistroAsync(Baz registro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un registro de balanza por su ID
    /// </summary>
    Task<bool> EliminarRegistroAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cambia el estado de un registro de balanza
    /// </summary>
    Task<bool> CambiarEstadoAsync(int id, int nuevoEstado, CancellationToken cancellationToken = default);
}
