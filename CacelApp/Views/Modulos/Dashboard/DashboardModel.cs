using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CacelApp.Views.Modulos.Dashboard.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Dashboard;

public partial class DashboardModel : ViewModelBase, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ISerialPortService _serialPortService;
    private readonly ICameraService _cameraService;

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
    
    [ObservableProperty]
    private ObservableCollection<CameraStreamInfo> _cameraStreams = new();
    
    [ObservableProperty]
    private CameraStreamInfo? _camaraSeleccionada;

    public DashboardModel(
        IConfigurationService configService, 
        ISerialPortService serialPortService,
        ICameraService cameraService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));

        // Suscribirse a cambios de configuración
        _configService.ConfigurationChanged += OnConfigurationChanged;

        // Inicializar de forma asíncrona
        InicializarAsync();
    }

    private async void InicializarAsync()
    {
        await LoadStatusDataAsync();
        await IniciarLecturaBalanzas();
    }

    private async void OnConfigurationChanged(object? sender, EventArgs e)
    {
        // Recargar datos cuando cambie la configuración
        await LoadStatusDataAsync();
        
        // Reiniciar lectura de balanzas con la nueva configuración
        DetenerLecturaBalanzas();
        await IniciarLecturaBalanzas();
    }

    private async Task LoadStatusDataAsync()
    {
        var sede = await _configService.GetSedeActivaAsync();
        
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

    private async Task IniciarLecturaBalanzas()
    {
        var sede = await _configService.GetSedeActivaAsync();
        if (sede != null && sede.Balanzas.Any())
        {
            // Solo suscribirse a eventos de peso (no iniciar lectura porque puede estar ya iniciada por otro módulo)
            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            
            // Obtener las últimas lecturas disponibles para mostrar valores actuales
            var ultimasLecturas = _serialPortService.ObtenerUltimasLecturas();
            if (ultimasLecturas.Any())
            {
                OnPesosLeidos(ultimasLecturas);
            }
            
            // Iniciar lectura solo si no está ya ejecutándose
            // El servicio internamente verifica si ya está ejecutando y no hace nada si es así
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
    
    /// <summary>
    /// Inicializa los streams de cámaras (debe llamarse desde el code-behind después de que los controles estén creados)
    /// </summary>
    public async Task IniciarStreamingCamarasAsync(Dictionary<int, IntPtr> handlesPorCanal)
    {
        var sede = await _configService.GetSedeActivaAsync();
        if (sede == null || !sede.Camaras.Any()) return;
        
        // Inicializar servicio de cámaras
        if (sede.Dvr != null)
        {
            var inicializado = await _cameraService.InicializarAsync(sede.Dvr, sede.Camaras.ToList());
            if (!inicializado) return;
        }
        
        // Crear información de streams para cada cámara activa
        var streams = new List<CameraStreamInfo>();
        foreach (var camara in sede.Camaras.Where(c => c.Activa))
        {
            var streamInfo = new CameraStreamInfo
            {
                Canal = camara.Canal,
                Nombre = string.IsNullOrEmpty(camara.Nombre) ? $"Cámara {camara.Canal}" : camara.Nombre,
                Ubicacion = camara.Ubicacion
            };
            
            // Si tenemos el handle de ventana para este canal, iniciar streaming
            if (handlesPorCanal.TryGetValue(camara.Canal, out var handle))
            {
                streamInfo.HandleVentana = handle;
                var playId = _cameraService.IniciarStreaming(camara.Canal, handle);
                streamInfo.StreamHandle = playId;
                streamInfo.IsStreaming = playId != IntPtr.Zero;
            }
            
            streams.Add(streamInfo);
        }
        
        CameraStreams = new ObservableCollection<CameraStreamInfo>(streams);
    }
    
    [RelayCommand]
    private void SeleccionarCamara(CameraStreamInfo? camara)
    {
        // Deseleccionar la anterior
        if (CamaraSeleccionada != null)
        {
            CamaraSeleccionada.IsSelected = false;
        }
        
        // Seleccionar la nueva
        CamaraSeleccionada = camara;
        if (CamaraSeleccionada != null)
        {
            CamaraSeleccionada.IsSelected = true;
        }
    }
    
    private void DetenerStreamingCamaras()
    {
        if (CameraStreams != null)
        {
            foreach (var stream in CameraStreams)
            {
                if (stream.IsStreaming)
                {
                    _cameraService.DetenerStreaming(stream.Canal);
                }
            }
        }
        
        _cameraService.Detener();
    }

    // Implementación de IDisposable para limpiar recursos
    public void Dispose()
    {
        // Desuscribirse del evento de configuración
        _configService.ConfigurationChanged -= OnConfigurationChanged;
        
        // Detener lectura de balanzas
        DetenerLecturaBalanzas();
        
        // Detener streaming de cámaras
        DetenerStreamingCamaras();
        
        GC.SuppressFinalize(this);
    }
}

