using Core.Repositories.Balanza.Entities;

namespace Core.Repositories.Balanza;

public interface IBalanzaSearchRepository
{
    Task<IEnumerable<Baz>> ObtenerTodosAsync(
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? vehiculoId = null,
        string? Agente = null,
        int? estado = null,
        CancellationToken cancellationToken = default);
}
