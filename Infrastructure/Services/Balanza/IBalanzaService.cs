using Core.Repositories.Balanza.Entities;

namespace Infrastructure.Services.Balanza;

/// <summary>
/// Interfaz para el servicio de escritura de balanza
/// Define operaciones de creación, actualización y eliminación
/// </summary>
public interface IBalanzaService
{
    Task<Baz> Balanza(Baz registro, CancellationToken cancellationToken = default);
}
