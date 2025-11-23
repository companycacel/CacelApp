namespace Core.Shared.Configuration;

/// <summary>
/// Configuración global de la aplicación (única para toda la app)
/// </summary>
public class GlobalConfig
{
    public string WebApiUrl { get; set; } = "";
    public FtpConfig Ftp { get; set; } = new();
}
