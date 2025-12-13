using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para gestionar cámaras conectadas a DVR Dahua
/// Basado en CacelTracking: Camara.cs líneas 36-154
/// </summary>
public interface ICameraService
{
    Task<bool> InicializarAsync(DvrConfig dvr, List<CamaraConfig> camaras);
    Task<MemoryStream?> CapturarImagenAsync(int canal);
    Task<List<(string nombre, MemoryStream stream)>> CapturarTodasAsync();
    Dictionary<int, bool> ObtenerEstadoCamaras();

    // Métodos para streaming en vivo
    IntPtr IniciarStreaming(int canal, IntPtr handleVentana);
    /// <summary>
    /// Detiene un stream específico por su handle
    /// </summary>
    void DetenerStreaming(IntPtr playId);

    /// <summary>
    /// Detiene todos los streams de una cámara específica
    /// </summary>
    void DetenerStreaming(int canal);
    Dictionary<int, IntPtr> ObtenerStreamsActivos();

    void Detener();
    
    /// <summary>
    /// Detiene completamente el SDK y libera todos los recursos.
    /// Solo debe llamarse al cerrar la aplicación.
    /// </summary>
    void DetenerCompletamente();
}
