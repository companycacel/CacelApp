using Core.Repositories.Login;
using Core.Repositories.Shared;
using WebApi.Helper;

namespace WebApi.Repositories.Shared;

public class FindFileRepository(IAuthService _authService) : IFindFileRepository
{
    public async Task<(byte[] File, string ContentType)> FindFile(object? request, CancellationToken cancellationToken = default)
    {
        var authenticatedClient = _authService.GetAuthenticatedClient();
        var req = BuildQueryParams.Make(request);
        var url = $"{req.Url}{(req.Id != null ? $"/{req.Id}" : "")}{(string.IsNullOrEmpty(req.Query) ? "" : $"?{req.Query}")}";

        authenticatedClient.DefaultRequestHeaders.Accept.Clear();
        authenticatedClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(req.Accept));
        var response = await authenticatedClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var file = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        return (file, contentType);
    }
}
