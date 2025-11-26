using Core.Repositories.Balanza;
using Core.Repositories.Balanza.Entities;
using Core.Shared.Entities;
using Core.Shared.Validators;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Servicio de aplicación para operaciones de escritura de balanza
/// Implementa la lógica de negocio y validaciones para crear/actualizar registros
/// </summary>
public class BalanzaService : IBalanzaService
{
    private readonly IBalanzaRepository _repository;

    public BalanzaService(IBalanzaRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Baz> Balanza(Baz registro, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarObjetoNoNulo(registro, nameof(registro));   
        if (registro.action == ActionType.Create.ToString() || registro.action == ActionType.Update.ToString())
        {
            // Validar que el registro sea válido
            if (!registro.EsValido())
                throw new InvalidOperationException("El registro de balanza no es válido");

            // Calcular peso neto si es necesario
            registro.CalcularPesoNeto();

        }
        if (registro.action == ActionType.Update.ToString() || registro.action == ActionType.Delete.ToString())
        {
            ValidationHelper.ValidarId(registro.baz_id, nameof(registro.baz_id));
        }
        // Asignar fechas
        registro.created = DateTime.UtcNow;
        registro.updated = DateTime.UtcNow;

        return await _repository.Balanza(registro, cancellationToken);
    }
}
