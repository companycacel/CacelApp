using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services.Shared;

public class SimpleFormFile : IFormFile
{
    private readonly byte[] _content;
    private readonly string _name;
    private readonly string _fileName;

    public SimpleFormFile(byte[] content, string name, string fileName)
    {
        _content = content;
        _name = name;
        _fileName = fileName;
    }

    public string ContentType => "image/jpeg";
    public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => _content.Length;
    public string Name => _name;
    public string FileName => _fileName;

    public void CopyTo(Stream target)
    {
        target.Write(_content, 0, _content.Length);
    }

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        return target.WriteAsync(_content, 0, _content.Length, cancellationToken);
    }

    public Stream OpenReadStream()
    {
        return new MemoryStream(_content);
    }
}
