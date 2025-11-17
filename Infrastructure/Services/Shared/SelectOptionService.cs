using Core.Repositories.Shared;
using Core.Shared.Entities;
using Core.Shared.Enums;
using Core.Shared.Validators;

namespace Infrastructure.Services.Shared;

/// <summary>
/// Implementación del servicio de listas de opciones
/// Delega al repositorio para obtener los datos
/// </summary>
public class SelectOptionService : ISelectOptionService
{
    private readonly ISelectOptionRepository _repository;

    public SelectOptionService(ISelectOptionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<SelectOption>> GetSelectOptionsAsync(
        SelectOptionType type, 
        int? code = null, 
        object? additionalParams = null,
        CancellationToken cancellationToken = default)
    {
        // Validación de parámetros si es necesario
        if (code.HasValue)
            ValidationHelper.ValidarId(code.Value, nameof(code));

        return await _repository.GetSelectOptionsAsync(type, code, additionalParams, cancellationToken);
    }
}
