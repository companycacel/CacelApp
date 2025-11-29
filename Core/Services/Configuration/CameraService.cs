using Core.Shared.Configuration;
using NetSDKCS;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para gestionar cámaras conectadas a DVR Dahua
/// Basado en CacelTracking: Camara.cs líneas 36-154
/// </summary>
public class CameraService : ICameraService
{
    private IntPtr _loginId = IntPtr.Zero;
    private readonly Dictionary<int, List<IntPtr>> _playIds = new();
    private readonly Dictionary<int, bool> _estadoCamaras = new();
    private NET_DEVICEINFO_Ex _deviceInfo = new();
    private bool _initialized = false;
    private bool _sdkInitialized = false;
    private List<CamaraConfig> _camaras = new();
    public async Task<bool> InicializarAsync(DvrConfig dvr, List<CamaraConfig> camaras)
    {
        try
        {
            // ✅ Si ya está inicializado y conectado, no hacer nada
            if (_loginId != IntPtr.Zero)
            {
                return true; 
            }

            // Inicializar SDK solo si no se ha hecho antes
            if (!_sdkInitialized)
            {
                var initResult = NETClient.Init(null, IntPtr.Zero, null);
                if (!initResult)
                {
                    var error = NETClient.GetLastError();
                    return false;
                }
                _sdkInitialized = true;
            }

            NET_DEVICEINFO_Ex deviceInfo = new NET_DEVICEINFO_Ex();

            _loginId = await Task.Run(() =>
                NETClient.LoginWithHighLevelSecurity(
                    dvr.Ip,
                    (ushort)(dvr.Puerto ?? 37777),
                    dvr.Usuario,
                    dvr.Password,
                    EM_LOGIN_SPAC_CAP_TYPE.TCP,
                    IntPtr.Zero,
                   ref deviceInfo
                )
            );

            if (_loginId == IntPtr.Zero)
            {
                var error = NETClient.GetLastError();
                return false;
            }

            _camaras = camaras;
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<MemoryStream?> CapturarImagenAsync(int canal)
    {
        if (!_playIds.TryGetValue(canal, out var playIds) || !playIds.Any())
        {
            return null;
        }

        var playId = playIds.First();

        try
        {
            string tempPath = Path.GetTempFileName();

            if (NETClient.CapturePicture(playId, tempPath, EM_NET_CAPTURE_FORMATS.JPEG))
            {
                var bytes = await File.ReadAllBytesAsync(tempPath);
                var ms = new MemoryStream(bytes);
                File.Delete(tempPath);
                return ms;
            }
        }
        catch
        {
            // Log error si es necesario
        }

        return null;
    }

    public async Task<List<(string nombre, MemoryStream stream)>> CapturarTodasAsync()
    {
        var capturas = new List<(string, MemoryStream)>();

        foreach (var canal in _estadoCamaras.Keys.Where(k => _estadoCamaras[k]))
        {
            var imagen = await CapturarImagenAsync(canal);
            if (imagen != null)
            {
                capturas.Add(($"camara_{canal}.jpg", imagen));
            }
        }

        return capturas;
    }

    public Dictionary<int, bool> ObtenerEstadoCamaras()
    {
        return new Dictionary<int, bool>(_estadoCamaras);
    }

    /// <summary>
    /// Inicia streaming en vivo de una cámara
    /// Basado en CacelTracking: Camara.cs línea 101
    /// </summary>
    public IntPtr IniciarStreaming(int canal, IntPtr handleVentana)
    {
        if (_playIds.ContainsKey(canal) && _playIds[canal].Any())
        {
            foreach (var oldPlayId in _playIds[canal].ToList())
            {
                NETClient.StopRealPlay(oldPlayId);
            }
            _playIds[canal].Clear();
        }
        if (_loginId == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        // No detener streams anteriores para permitir múltiples vistas (thumbnail + ampliada)
        
        try
        {
            // Iniciar reproducción en vivo (canal - 1 porque el SDK usa base 0)
            IntPtr playId = NETClient.RealPlay(_loginId, canal - 1, handleVentana);

            if (playId != IntPtr.Zero)
            {
                if (!_playIds.ContainsKey(canal))
                {
                    _playIds[canal] = new List<IntPtr>();
                }
                _playIds[canal].Add(playId);
                _estadoCamaras[canal] = true;
                return playId;
            }
            else
            {
                // Solo marcar como inactiva si no hay otros streams
                if (!_playIds.ContainsKey(canal) || !_playIds[canal].Any())
                {
                    _estadoCamaras[canal] = false;
                }
                return IntPtr.Zero;
            }
        }
        catch
        {
            _estadoCamaras[canal] = false;
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Detiene el streaming de una cámara específica
    /// </summary>
    /// <summary>
    /// Detiene un stream específico por su handle
    /// </summary>
    public void DetenerStreaming(IntPtr playId)
    {
        if (playId == IntPtr.Zero) return;

        try
        {
            NETClient.StopRealPlay(playId);
        }
        catch { }

        // Buscar y remover de la lista
        foreach (var canal in _playIds.Keys.ToList())
        {
            if (_playIds[canal].Contains(playId))
            {
                _playIds[canal].Remove(playId);
                if (!_playIds[canal].Any())
                {
                    _estadoCamaras[canal] = false;
                }
                break;
            }
        }
    }

    /// <summary>
    /// Detiene todos los streams de una cámara específica
    /// </summary>
    public void DetenerStreaming(int canal)
    {
        if (_playIds.TryGetValue(canal, out var playIds))
        {
            foreach (var playId in playIds.ToList())
            {
                try
                {
                    NETClient.StopRealPlay(playId);
                }
                catch { }
            }

            _playIds.Remove(canal);
            _estadoCamaras[canal] = false;
        }
    }

    /// <summary>
    /// Obtiene todos los streams activos
    /// </summary>
    public Dictionary<int, IntPtr> ObtenerStreamsActivos()
    {
        var result = new Dictionary<int, IntPtr>();
        foreach (var kvp in _playIds)
        {
            if (kvp.Value.Any())
            {
                result[kvp.Key] = kvp.Value.First();
            }
        }
        return result;
    }

    public void Detener()

    {
        try
        {
            // Detener reproducción de todas las cámaras
            foreach (var playIds in _playIds.Values)
            {
                foreach (var playId in playIds)
                {
                    try
                    {
                        NETClient.StopRealPlay(playId);
                    }
                    catch { }
                }
            }

            _playIds.Clear();

            // Logout del DVR
            if (_loginId != IntPtr.Zero)
            {
                NETClient.Logout(_loginId);
                _loginId = IntPtr.Zero;
            }

            // Cleanup del SDK
            if (_initialized)
            {
                NETClient.Cleanup();
                _initialized = false;
            }
        }
        catch
        {
            // Ignorar errores al detener
        }
    }
}
