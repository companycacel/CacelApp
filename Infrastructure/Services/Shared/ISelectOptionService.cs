using Core.Shared.Entities;
using Core.Shared.Enums;

namespace Infrastructure.Services.Shared;

/// <summary>
/// Servicio de aplicación para obtener listas de opciones
/// </summary>
public interface ISelectOptionService
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
