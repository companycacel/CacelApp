using Core.Repositories.Balanza.Entities;

namespace Core.Repositories.Balanza;

public interface IBalanzaRepository
{
    Task<Baz> Balanza(Baz registro, CancellationToken cancellationToken = default);

}
