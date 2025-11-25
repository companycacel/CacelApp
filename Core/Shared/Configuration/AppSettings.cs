namespace Core.Shared.Configuration;

/// <summary>
/// Modelo para deserializar appsettings.json
/// </summary>
public class AppSettings
{
    public ApiUrlsConfig ApiUrls { get; set; } = new();
}

/// <summary>
/// URLs de API por entorno
/// </summary>
public class ApiUrlsConfig
{
    public string Development { get; set; } = string.Empty;
    public string Production { get; set; } = string.Empty;
}
