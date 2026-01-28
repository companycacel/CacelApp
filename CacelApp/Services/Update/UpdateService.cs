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
    private UpdateInfo? _lastUpdateInfo;

    public UpdateService(Core.Services.Configuration.IConfigurationService configService)
    {
        try
        {
            var appSettings = configService.LoadAppSettings();
            _updateUrl = appSettings.UpdateSettings?.UpdateUrl ?? string.Empty;

            // Solo inicializar UpdateManager si hay una URL configurada
            if (!string.IsNullOrEmpty(_updateUrl))
            {
                var source = new SimpleWebSource(_updateUrl);
                _updateManager = new UpdateManager(source);
            }
        }
        catch (Exception ex)
        {
            // Si falla la inicialización, el servicio estará deshabilitado
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
            // Si no hay UpdateManager configurado, no hay actualizaciones
            if (_updateManager == null)
                return null;

            var updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (updateInfo == null)
                return null;

            // Guardar la información de actualización para usarla después
            _lastUpdateInfo = new UpdateInfo
            {
                Version = updateInfo.TargetFullRelease.Version.ToString(),
                SizeBytes = updateInfo.TargetFullRelease.Size,
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
            // Primero verificar actualizaciones para obtener el UpdateInfo de Velopack
            var velopackUpdateInfo = await _updateManager.CheckForUpdatesAsync();

            if (velopackUpdateInfo == null)
                throw new InvalidOperationException("No se encontró información de actualización");

            // Descargar la actualización con reporte de progreso
            await _updateManager.DownloadUpdatesAsync(velopackUpdateInfo, progress: (p) =>
            {
                progress?.Report(p);
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error descargando actualización: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Aplica actualizaciones pendientes y reinicia la aplicación
    /// </summary>
    public async Task ApplyUpdatesAndRestartAsync()
    {
        if (_updateManager == null)
            throw new InvalidOperationException("El servicio de actualizaciones no está disponible");

        try
        {
            // Aplicar la actualización y reiniciar (pasar null para usar la última versión descargada)
            _updateManager.ApplyUpdatesAndRestart(null);

            // Este código no se ejecutará porque la app se reiniciará
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
}
