
using Core.Shared.Entities;

namespace Core.Repositories.Shared;

public interface IFindFileRepository
{
    Task<(byte[] File, string ContentType)> FindFile(object? request, CancellationToken cancellationToken = default);
}
