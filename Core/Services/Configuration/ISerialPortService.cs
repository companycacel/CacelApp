using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para lectura continua de balanzas por puerto serial
/// Basado en implementación de CacelTracking
/// </summary>
public interface ISerialPortService
{
    void IniciarLectura(List<BalanzaConfig> balanzas);
    void DetenerLectura();
    Dictionary<string, string> ObtenerUltimasLecturas();
    event Action<Dictionary<string, string>>? OnPesosLeidos;
}
