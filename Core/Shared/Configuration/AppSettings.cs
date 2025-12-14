namespace Core.Shared.Configuration;

/// <summary>
/// Modelo para deserializar appsettings.json
/// </summary>
public class AppSettings
{
    public ApiUrlsConfig ApiUrls { get; set; } = new();
    public UpdateSettingsConfig? UpdateSettings { get; set; }
}

/// <summary>
/// URLs de API por entorno
/// </summary>
public class ApiUrlsConfig
{
    public string Development { get; set; } = string.Empty;
    public string Production { get; set; } = string.Empty;
}

/// <summary>
/// Configuración de actualizaciones automáticas
/// </summary>
public class UpdateSettingsConfig
{
    /// <summary>
    /// URL del servidor de actualizaciones
    /// </summary>
    public string? UpdateUrl { get; set; }

    /// <summary>
    /// Verificar actualizaciones al iniciar la aplicación
    /// </summary>
    public bool CheckOnStartup { get; set; } = true;

    /// <summary>
    /// Canal de actualizaciones (stable, beta, etc.)
    /// </summary>
    public string Channel { get; set; } = "stable";
}
