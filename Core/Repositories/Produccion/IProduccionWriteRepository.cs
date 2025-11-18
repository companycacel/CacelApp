using Core.Shared.Entities.Generic;

namespace Core.Repositories.Produccion;

/// <summary>
/// Interfaz que define el contrato para operaciones de escritura de producción
/// Implementa el patrón Repository y separación de responsabilidades (CQRS)
/// </summary>
public interface IProduccionWriteRepository
{
    /// <summary>
    /// Crea un nuevo registro de producción
    /// </summary>
    /// <param name="registro">Datos del registro de producción</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Registro de producción creado</returns>
    Task<Pde> CrearAsync(Pde registro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un registro de producción existente
    /// </summary>
    /// <param name="registro">Datos del registro de producción</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Registro de producción actualizado</returns>
    Task<Pde> ActualizarAsync(Pde registro, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina (anula) un registro de producción por su ID
    /// </summary>
    /// <param name="id">ID del registro de producción</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si se eliminó correctamente</returns>
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}
