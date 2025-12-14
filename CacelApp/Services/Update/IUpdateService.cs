namespace CacelApp.Services.Update;

/// <summary>
/// Servicio para gestionar actualizaciones de la aplicación usando Velopack
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Verifica si hay actualizaciones disponibles
    /// </summary>
    /// <returns>Información de la actualización disponible, o null si no hay actualizaciones</returns>
    Task<UpdateInfo?> CheckForUpdatesAsync();

    /// <summary>
    /// Descarga e instala una actualización
    /// </summary>
    /// <param name="updateInfo">Información de la actualización a instalar</param>
    /// <param name="progress">Callback para reportar progreso (0-100)</param>
    Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null);

    /// <summary>
    /// Aplica actualizaciones pendientes y reinicia la aplicación
    /// </summary>
    Task ApplyUpdatesAndRestartAsync();

    /// <summary>
    /// Verifica si hay actualizaciones pendientes de aplicar
    /// </summary>
    bool HasPendingUpdates();
}
