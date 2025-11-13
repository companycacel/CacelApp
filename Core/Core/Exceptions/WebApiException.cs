using System.Net;

namespace Core.Exceptions;

public class WebApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ErrorDetails { get; }

    public WebApiException(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string details = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorDetails = details;
    }

    public WebApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = HttpStatusCode.InternalServerError;
    }
}
