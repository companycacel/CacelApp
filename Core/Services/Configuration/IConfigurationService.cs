using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para gestionar la configuración de la aplicación
/// Guarda en AppData\Local\CacelApp\config.json
/// </summary>
public interface IConfigurationService
{
    // Cargar/Guardar
    Task<AppConfiguration> LoadAsync();
    Task SaveAsync(AppConfiguration config);

    // Backup
    Task CreateBackupAsync();
    Task<AppConfiguration> RestoreBackupAsync();

    // Export/Import
    Task<string> ExportAsync(string? filePath = null);
    Task<AppConfiguration> ImportAsync(string filePath);

    // Sede activa
    Task<SedeConfig?> GetSedeActivaAsync();
    Task SetSedeActivaAsync(int sedeId);

    // Configuración actual en memoria
    AppConfiguration? CurrentConfiguration { get; }

    // AppSettings (URLs de API por entorno)
    AppSettings LoadAppSettings();
    string GetCurrentApiUrl();

    // Evento para notificar cambios en la configuración
    event EventHandler? ConfigurationChanged;
}
