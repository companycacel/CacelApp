using System.Net;

namespace Core.Exceptions;

public class WebApiException : Exception
{
    public HttpStatusCode StatusCode { get; } // 401
    public string ErrorType { get; } // "Unauthorized"
    public string ErrorDetails { get; } // "Contraseña Incorrecta"

    /// <summary>
    /// Crea una excepción basada en una respuesta de Web API fallida.
    /// </summary>
    /// <param name="message">El mensaje principal del error (ej. "Contraseña Incorrecta")</param>
    /// <param name="statusCode">El código HTTP (ej. 401)</param>
    /// <param name="errorType">El tipo de error retornado (ej. "Unauthorized")</param>
    public WebApiException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string errorType = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        ErrorDetails = message;
    }

    /// <summary>
    /// Sobrescribe la propiedad Message para incluir el código de estado.
    /// </summary>
    public override string Message => $"API Error ({StatusCode}): {base.Message}";
}
