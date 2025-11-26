using Core.Repositories.Balanza.Entities;

namespace Core.Repositories.Balanza;

/// <summary>
/// Interfaz que define el contrato para operaciones de escritura de registros de balanza
/// </summary>
public interface IBalanzaRepository
{
    Task<Baz> Balanza(Baz registro, CancellationToken cancellationToken = default);

}
