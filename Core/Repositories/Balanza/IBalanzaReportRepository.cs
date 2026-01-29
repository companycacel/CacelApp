

namespace Core.Repositories.Balanza;

public interface IBalanzaReportRepository
{
    Task<byte[]> GenerarReportePdfAsync(int registroId, CancellationToken cancellationToken = default);

}