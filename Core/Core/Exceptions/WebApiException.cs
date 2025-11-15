using System.Net;

namespace Core.Exceptions;

public class WebApiException : Exception
{
    public string StatusCode { get; } // 401
    public string ErrorType { get; } // "Unauthorized"
    public string ErrorDetails { get; } // "Contraseña Incorrecta"

    /// <summary>
    /// Crea una excepción basada en una respuesta de Web API fallida.
    /// </summary>
    /// <param name="message">El mensaje principal del error (ej. "Contraseña Incorrecta")</param>
    /// <param name="statusCode">El código HTTP (ej. 401)</param>
    /// <param name="errorType">El tipo de error retornado (ej. "Unauthorized")</param>
    /// 

    public WebApiException(
        string message,
        int statusCode,
        string errorType = null)
        : base(message)
    {
        StatusCode = MapStatus(statusCode).ToString();
        ErrorType = errorType;
        ErrorDetails = message;
    }

    /// <summary>
    /// Sobrescribe la propiedad Message para incluir el código de estado.
    /// </summary>
    public override string Message => $"API Error ({StatusCode}): {base.Message}";
    public static HttpStatusCode MapStatus(int code)
    {
        return code switch
        {
            400 => HttpStatusCode.BadRequest,
            401 => HttpStatusCode.Unauthorized,
            403 => HttpStatusCode.Forbidden,
            404 => HttpStatusCode.NotFound,
            408 => HttpStatusCode.RequestTimeout,
            409 => HttpStatusCode.Conflict,
            501 => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
