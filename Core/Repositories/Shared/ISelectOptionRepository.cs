using Core.Shared.Entities;
using Core.Shared.Enums;

namespace Core.Repositories.Shared;

/// <summary>
/// Repositorio para obtener listas de opciones desde la capa de datos
/// </summary>
public interface ISelectOptionRepository
{
    /// <summary>
    /// Obtiene una lista de opciones según el tipo especificado
    /// </summary>
    /// <param name="type">Tipo de lista de opciones</param>
    /// <param name="code">Código opcional para filtrar</param>
    /// <param name="additionalParams">Parámetros adicionales</param>
    /// <returns>Colección de opciones</returns>
    Task<IEnumerable<SelectOption>> GetSelectOptionsAsync(
        SelectOptionType type, 
        int? code = null, 
        object? additionalParams = null,
        CancellationToken cancellationToken = default);
}
