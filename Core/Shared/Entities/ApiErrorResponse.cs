
namespace Core.Shared.Entities;

public class ApiErrorResponse
{
    public string message { get; set; }
    public string error { get; set; }
    public int statusCode { get; set; }
}
