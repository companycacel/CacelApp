
using Core.Repositories.Shared;
using Core.Shared.Entities;
using Core.Shared.Validators;
using Microsoft.Win32;

namespace Services.Shared;

public class FindFileService(IFindFileRepository findFileRepository) : IFindFileService
{

    public async Task<(byte[] File, string ContentType)> FindFile(object? request, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ValidarObjetoNoNulo(request, nameof(request));
        return await findFileRepository.FindFile(request, cancellationToken);
    }
}
