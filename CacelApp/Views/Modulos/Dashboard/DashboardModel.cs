using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Services.Configuration;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Dashboard;

public partial class DashboardModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly ISerialPortService _serialPortService;

    [ObservableProperty]
    private ObservableCollection<BalanzaStatus> _balanzas;

    [ObservableProperty]
    private ObservableCollection<BalanzaStatus> _camaraStatus;

    [ObservableProperty]
    private string _pesajesHoy = "1,204"; // TODO: Obtener de servicio real

    [ObservableProperty]
    private string _produccionMes = "45.2 Ton"; // TODO: Obtener de servicio real

    [ObservableProperty]
    private string _sistemaStatus = "Operativo";

    public DashboardModel(IConfigurationService configService, ISerialPortService serialPortService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));

        LoadStatusData();
        IniciarLecturaBalanzas();
    }

    private void LoadStatusData()
    {
        var sede = _configService.GetSedeActiva();
        
        if (sede == null)
        {
            Balanzas = new ObservableCollection<BalanzaStatus>();
            CamaraStatus = new ObservableCollection<BalanzaStatus>();
            SistemaStatus = "Sin configuración";
            return;
        }

        // Cargar balanzas configuradas
        Balanzas = new ObservableCollection<BalanzaStatus>(
            sede.Balanzas.Select(b => new BalanzaStatus
            {
                Name = b.Nombre,
                Puerto = b.Puerto,
                Camaras = new List<int>(), // Las cámaras están a nivel de sede, no de balanza individual
                StatusText = "Esperando datos...",
                IsOnline = false, // Se actualizará cuando lleguen datos
                IconKind = PackIconKind.ScaleBalance,
                CurrentWeight = 0
            })
        );

        // Cargar estado de cámaras configuradas (a nivel de sede)
        var camarasStatus = sede.Camaras
            .Select(c => new BalanzaStatus
            {
                Name = string.IsNullOrEmpty(c.Nombre) ? $"Cámara {c.Canal}" : c.Nombre,
                StatusText = string.IsNullOrEmpty(c.Ubicacion) ? $"Canal {c.Canal}" : c.Ubicacion,
                IsOnline = c.Activa, // Usar estado de configuración
                IconKind = PackIconKind.Video,
                Puerto = "",
                Camaras = new List<int> { c.Canal }
            })
            .ToList();

        CamaraStatus = new ObservableCollection<BalanzaStatus>(camarasStatus);
        
        SistemaStatus = sede.Balanzas.Any() ? "Operativo" : "Sin balanzas";
    }

    private void IniciarLecturaBalanzas()
    {
        var sede = _configService.GetSedeActiva();
        if (sede != null && sede.Balanzas.Any())
        {
            // Suscribirse a eventos de peso
            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            _serialPortService.IniciarLectura(sede.Balanzas);
        }
    }

    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        // Actualizar propiedades en el hilo de la UI
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var lectura in lecturas)
            {
                // Buscar la balanza por puerto
                var balanza = Balanzas.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanza != null)
                {
                    // Actualizar peso
                    if (decimal.TryParse(lectura.Value, out decimal peso))
                    {
                        balanza.CurrentWeight = peso;
                        balanza.IsOnline = true;
                        balanza.StatusText = "En línea";
                    }
                }
            }
        });
    }

    public void DetenerLecturaBalanzas()
    {
        _serialPortService.OnPesosLeidos -= OnPesosLeidos;
        _serialPortService.DetenerLectura();
    }

    // Cleanup cuando se destruye el ViewModel
    ~DashboardModel()
    {
        DetenerLecturaBalanzas();
    }
}

