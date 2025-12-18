using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para lectura continua de balanzas por puerto serial
/// Basado en implementación de CacelTracking
/// </summary>
public interface ISerialPortService
{
    void IniciarLectura(IEnumerable<BalanzaConfig> balanzas, TipoSede tipoSede);
    void DetenerLectura();
    Dictionary<string, string> ObtenerUltimasLecturas();
    Dictionary<string, bool> ObtenerEstabilidadActual();
    event Action<Dictionary<string, string>>? OnPesosLeidos;
    event Action<Dictionary<string, bool>>? OnEstabilidadCambiada;
}
