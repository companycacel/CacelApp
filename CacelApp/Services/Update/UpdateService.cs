using System.Diagnostics;
using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace CacelApp.Services.Update;

/// <summary>
/// Implementación del servicio de actualizaciones usando Velopack
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly UpdateManager? _updateManager;
    private readonly string _updateUrl;
    private Velopack.UpdateInfo? _velopackUpdateInfo;
    private UpdateInfo? _lastUpdateInfo;

    public UpdateService(Core.Services.Configuration.IConfigurationService configService)
    {
        try
        {
            var appSettings = configService.LoadAppSettings();
            _updateUrl = appSettings.UpdateSettings?.UpdateUrl ?? string.Empty;

            if (!string.IsNullOrEmpty(_updateUrl))
            {
                var source = new SimpleWebSource(_updateUrl);
                _updateManager = new UpdateManager(source);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error inicializando UpdateService: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si hay actualizaciones disponibles
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            if (_updateManager == null)
                return null;

            _velopackUpdateInfo = await _updateManager.CheckForUpdatesAsync();

            if (_velopackUpdateInfo == null)
                return null;

            _lastUpdateInfo = new UpdateInfo
            {
                Version = _velopackUpdateInfo.TargetFullRelease.Version.ToString(),
                SizeBytes = _velopackUpdateInfo.TargetFullRelease.Size,
                ReleaseNotes = null, // VelopackAsset no tiene ReleaseNotes en esta versión
                DownloadUrl = _updateUrl,
                PublishedAt = null // VelopackAsset no tiene PublishedAt en esta versión
            };

            return _lastUpdateInfo;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error verificando actualizaciones: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Descarga e instala una actualización
    /// </summary>
    public async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        if (_updateManager == null)
            throw new InvalidOperationException("El servicio de actualizaciones no está disponible");

        try
        {
            if (_velopackUpdateInfo == null)
            {
                _velopackUpdateInfo = await _updateManager.CheckForUpdatesAsync();
            }

            if (_velopackUpdateInfo == null)
                throw new InvalidOperationException("No se encontró información de actualización en el servidor");

            await _updateManager.DownloadUpdatesAsync(_velopackUpdateInfo, progress: (p) =>
            {
                progress?.Report(p);
            });
        }
        catch (Exception ex)
        {
            _velopackUpdateInfo = null;
            throw new InvalidOperationException($"Error descargando actualización: {ex.Message} (Verifica que el servidor permita descargar archivos .nupkg)", ex);
        }
    }

    /// <summary>
    /// Aplica actualizaciones pendientes y reinicia la aplicación
    /// </summary>
    public async Task ApplyUpdatesAndRestartAsync()
    {
        if (_updateManager == null)
            throw new InvalidOperationException("El servicio de actualizaciones no está disponible");

        if (!_updateManager.IsInstalled)
            throw new InvalidOperationException("Las actualizaciones automáticas solo están disponibles en la versión instalada de la aplicación.");

        try
        {
            App.ReleaseSingleInstanceMutex();

            // Aplicar la actualización y reiniciar
            _updateManager.ApplyUpdatesAndRestart(_velopackUpdateInfo?.TargetFullRelease);

            // Este código no se ejecutará si la app se reinicia correctamente
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error aplicando actualización: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Verifica si hay actualizaciones pendientes de aplicar
    /// </summary>
    public bool HasPendingUpdates()
    {
        try
        {
            return _updateManager?.IsUpdatePendingRestart ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene la versión actual de la aplicación
    /// </summary>
    public string CurrentVersion
    {
        get
        {
            if (_updateManager != null && _updateManager.IsInstalled)
            {
                return _updateManager.CurrentVersion?.ToString() ?? "0.0.0";
            }
            else
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null) return "1.0.0";
                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi.ProductVersion?.Split('+')[0] ?? "1.0.0";
            }
        }
    }
}
