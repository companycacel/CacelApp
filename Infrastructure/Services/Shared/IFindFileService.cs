using Core.Shared.Entities;

namespace Services.Shared;

public interface IFindFileService
{    /// <summary>
     /// retorna todo tipo de archivo
     /// </summary>
    Task<(byte[] File, string ContentType)> FindFile(object? request, CancellationToken cancellationToken = default);
}
