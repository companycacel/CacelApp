namespace Core.Services.Configuration;

/// <summary>
/// Resultado de una prueba de conexión
/// </summary>
public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}
