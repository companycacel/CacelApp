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
    void Detener();
}
