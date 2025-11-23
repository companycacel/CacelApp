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
    private readonly Dictionary<int, IntPtr> _playIds = new();
    private readonly Dictionary<int, bool> _estadoCamaras = new();
    private NET_DEVICEINFO_Ex _deviceInfo = new();
    private bool _initialized = false;
    
    public async Task<bool> InicializarAsync(DvrConfig dvr, List<CamaraConfig> camaras)
    {
        try
        {
            // Inicializar SDK Dahua
            if (!_initialized)
            {
                NETClient.Init(null, IntPtr.Zero, null);
                _initialized = true;
            }
            
            // Login al DVR Dahua
            _loginId = NETClient.LoginWithHighLevelSecurity(
                dvr.Ip,
                (ushort)dvr.Puerto,
                dvr.Usuario,
                dvr.Password,
                EM_LOGIN_SPAC_CAP_TYPE.TCP,
                IntPtr.Zero,
                ref _deviceInfo
            );
            
            if (_loginId == IntPtr.Zero)
            {
                var error = NETClient.GetLastError();
                return false;
            }
            
            // Actualizar estado del DVR
            dvr.Conectado = true;
            dvr.UltimaConexion = DateTime.Now;
            
            // Marcar cámaras como conectadas
            foreach (var camara in camaras.Where(c => c.Activa))
            {
                _estadoCamaras[camara.Canal] = true;
                camara.Conectada = true;
            }
            
            return await Task.FromResult(true);
        }
        catch (Exception)
        {
            dvr.Conectado = false;
            return false;
        }
    }
    
    public async Task<MemoryStream?> CapturarImagenAsync(int canal)
    {
        if (!_playIds.TryGetValue(canal, out var playId))
        {
            // Si no hay playID, intentar crear uno temporal para captura
            // En producción, necesitarías un handle de ventana
            return null;
        }
        
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
    
    public void Detener()
    {
        try
        {
            // Detener reproducción de todas las cámaras
            foreach (var playId in _playIds.Values)
            {
                try
                {
                    NETClient.StopRealPlay(playId);
                }
                catch { }
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
