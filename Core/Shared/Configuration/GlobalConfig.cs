namespace Core.Shared.Configuration;

/// <summary>
/// Configuración global de la aplicación (única para toda la app)
/// </summary>
public class GlobalConfig
{
    /// <summary>
    /// Entorno de ejecución: "Development" o "Production"
    /// </summary>
    public string Environment { get; set; } = "Development";
    
    public FtpConfig Ftp { get; set; } = new();
}
