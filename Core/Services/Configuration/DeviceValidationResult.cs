namespace Core.Services.Configuration;

/// <summary>
/// Resultado de validación de dispositivos para un módulo
/// </summary>
public class DeviceValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
