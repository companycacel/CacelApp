using CacelApp.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using Core.Shared.Configuration;
using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace CacelApp.Views.Modulos.Configuracion;

public partial class ConfiguracionModel : ViewModelBase
{
    private readonly IConfigurationService _configService;
    private readonly IConnectionTestService _connectionTestService;
    private readonly ISerialPortService _serialPortService;
    private readonly ICameraService _cameraService;
    private readonly IDialogService _dialogService;
    private readonly ILoadingService _loadingService;

    [ObservableProperty]
    private AppConfiguration _appConfig;

    [ObservableProperty]
    private SedeConfig? _sedeSeleccionada;
    
    partial void OnSedeSeleccionadaChanged(SedeConfig? value)
    {
        System.Diagnostics.Debug.WriteLine($"[ConfiguracionModel] OnSedeSeleccionadaChanged llamado. Valor: {value?.Nombre ?? "NULL"}");
        if (value != null)
        {
            System.Diagnostics.Debug.WriteLine($"  - Sede ID: {value.Id}");
            System.Diagnostics.Debug.WriteLine($"  - Balanzas count: {value.Balanzas?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  - Camaras count: {value.Camaras?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  - DVR IP: {value.Dvr?.Ip ?? "NULL"}");
        }
        
        // Notificar cambios en propiedades computadas
        OnPropertyChanged(nameof(MaxBalanzasPermitidas));
        OnPropertyChanged(nameof(MostrarCamaras));
    }

    [ObservableProperty]
    private BalanzaConfig? _balanzaSeleccionada;


    [ObservableProperty]
    private CamaraConfig? _camaraSeleccionada;

    /// <summary>
    /// Número máximo de balanzas permitidas para la sede seleccionada
    /// </summary>
    public int MaxBalanzasPermitidas => SedeSeleccionada?.GetMaxBalanzas() ?? 0;
    
    /// <summary>
    /// Indica si se deben mostrar las cámaras según el tipo de sede
    /// </summary>
    public bool MostrarCamaras => SedeSeleccionada?.RequiereCamaras() ?? false;

    /// <summary>
    /// Tipos de sede disponibles para el ComboBox (mapeo automático desde enum)
    /// </summary>
    public ObservableCollection<Core.Shared.Entities.SelectOption> TiposDeSede { get; } = new ObservableCollection<Core.Shared.Entities.SelectOption>(
        Enum.GetValues(typeof(TipoSede))
            .Cast<TipoSede>()
            .Select(tipo => new Core.Shared.Entities.SelectOption 
            { 
                Label = tipo.ToString(), 
                Value = tipo 
            })
    );

    /// <summary>
    /// Entorno actual (Development o Production)
    /// </summary>
    public string EntornoActual => AppConfig.Global?.Environment ?? "Development";

    /// <summary>
    /// URL de la API actual (calculada según el entorno)
    /// </summary>
    public string ApiUrlActual => _configService.GetCurrentApiUrl();

    private string _entornoOriginal;

    /// <summary>
    /// Indica si está en modo producción (para checkbox)
    /// </summary>
    public bool EsProduccion
    {
        get => AppConfig.Global?.Environment == "Production";
        set
        {
            if (AppConfig.Global != null)
            {
                AppConfig.Global.Environment = value ? "Production" : "Development";
                OnPropertyChanged(nameof(EsProduccion));
                OnPropertyChanged(nameof(EntornoActual));
                OnPropertyChanged(nameof(ApiUrlActual));
                OnPropertyChanged(nameof(EntornoCambiado));
            }
        }
    }

    /// <summary>
    /// Indica si el entorno fue cambiado y requiere reinicio
    /// </summary>
    public bool EntornoCambiado => _entornoOriginal != null && _entornoOriginal != EntornoActual;

    [ObservableProperty]
    private ObservableCollection<string> _puertosDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<int> _baudRates = new ObservableCollection<int>(new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 });

    public ConfiguracionModel(
        IConfigurationService configService,
        IConnectionTestService connectionTestService,
        ISerialPortService serialPortService,
        ICameraService cameraService,
        IDialogService dialogService,
        ILoadingService loadingService)
    {
        _configService = configService;
        _connectionTestService = connectionTestService;
        _serialPortService = serialPortService;
        _cameraService = cameraService;
        _dialogService = dialogService;
        _loadingService = loadingService;

        _appConfig = new AppConfiguration();
        
        // Cargar puertos seriales disponibles
        ActualizarPuertosDisponibles();
        
        // Cargar configuración inicial
        CargarConfiguracionAsync();
    }

    private void ActualizarPuertosDisponibles()
    {
        PuertosDisponibles.Clear();
        foreach (var port in System.IO.Ports.SerialPort.GetPortNames())
        {
            PuertosDisponibles.Add(port);
        }
    }

    private async void CargarConfiguracionAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[ConfiguracionModel] Iniciando carga de configuración...");
            _loadingService.StartLoading();
            var loadedConfig = await _configService.LoadAsync();
            System.Diagnostics.Debug.WriteLine($"[ConfiguracionModel] Configuración cargada. Sedes count: {loadedConfig.Sedes.Count}");
            
            // Actualizar propiedades en lugar de reemplazar la instancia
            // Esto preserva los bindings de WPF
            AppConfig.EquipoNombre = loadedConfig.EquipoNombre;
            AppConfig.Version = loadedConfig.Version;
            AppConfig.UltimaActualizacion = loadedConfig.UltimaActualizacion;
            AppConfig.SedeActivaId = loadedConfig.SedeActivaId;
            
            // Actualizar Global
            AppConfig.Global = loadedConfig.Global;
            
            // Guardar entorno original para detectar cambios
            _entornoOriginal = AppConfig.Global?.Environment ?? "Development";
            
            // Actualizar Sedes (limpiar y agregar)
            AppConfig.Sedes.Clear();
            foreach (var sede in loadedConfig.Sedes)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfiguracionModel] Agregando sede: {sede.Nombre} (ID: {sede.Id})");
                AppConfig.Sedes.Add(sede);
            }
            
            System.Diagnostics.Debug.WriteLine($"[ConfiguracionModel] Total sedes en AppConfig: {AppConfig.Sedes.Count}");
            
            if (AppConfig.Sedes.Any())
            {
                var sedeActiva = AppConfig.GetSedeActiva() ?? AppConfig.Sedes.First();
                System.Diagnostics.Debug.WriteLine($"[ConfiguracionModel] Seleccionando sede: {sedeActiva.Nombre} (ID: {sedeActiva.Id})");
                SedeSeleccionada = sedeActiva;
                System.Diagnostics.Debug.WriteLine($"[ConfiguracionModel] SedeSeleccionada asignada: {SedeSeleccionada?.Nombre}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ConfiguracionModel] WARNING: No hay sedes disponibles");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError($"Error al cargar configuración: {ex.Message}");
        }
        finally
        {
            _loadingService.StopLoading();
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            _loadingService.StartLoading();
            
            // Validar configuración actual
            if (SedeSeleccionada != null && !SedeSeleccionada.EsValida())
            {
                string validationMessage = SedeSeleccionada.GetValidationMessage();
                await _dialogService.ShowError($"La configuración de la sede no es válida: {validationMessage}");
                return;
            }

            await _configService.SaveAsync(AppConfig);
            
            // Actualizar entorno original después de guardar
            _entornoOriginal = AppConfig.Global?.Environment ?? "Development";
            OnPropertyChanged(nameof(EntornoCambiado));
            
            await _dialogService.ShowSuccess("Configuración guardada exitosamente.");
            
            // Reiniciar servicios si es necesario
            //_serialPortService.Reiniciar();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError($"Error al guardar: {ex.Message}");
        }
        finally
        {
            _loadingService.StopLoading();
        }
    }

    [RelayCommand]
    private async Task ExportarAsync()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"cacel_config_{DateTime.Now:yyyyMMdd}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _loadingService.StartLoading();
                // Usamos el servicio para exportar directamente al archivo seleccionado
                await _configService.ExportAsync(saveFileDialog.FileName);
                await _dialogService.ShowSuccess("Configuración exportada correctamente.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError($"Error al exportar: {ex.Message}");
        }
        finally
        {
            _loadingService.StopLoading();
        }
    }

    [RelayCommand]
    private async Task ImportarAsync()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (await _dialogService.ShowConfirm("Confirmación", "¿Está seguro de importar esta configuración? Se sobrescribirá la configuración actual."))
                {
                    _loadingService.StartLoading();
                    var importedConfig = await _configService.ImportAsync(openFileDialog.FileName);
                    
                    // Actualizar propiedades en lugar de reemplazar la instancia
                    AppConfig.EquipoNombre = importedConfig.EquipoNombre;
                    AppConfig.Version = importedConfig.Version;
                    AppConfig.UltimaActualizacion = importedConfig.UltimaActualizacion;
                    AppConfig.SedeActivaId = importedConfig.SedeActivaId;
                    AppConfig.Global = importedConfig.Global;
                    
                    // Actualizar Sedes
                    AppConfig.Sedes.Clear();
                    foreach (var sede in importedConfig.Sedes)
                    {
                        AppConfig.Sedes.Add(sede);
                    }
                    
                    if (AppConfig.Sedes.Any())
                    {
                        SedeSeleccionada = AppConfig.GetSedeActiva() ?? AppConfig.Sedes.First();
                    }
                    
                    await _dialogService.ShowSuccess("Configuración importada correctamente.");
                }
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError($"Error al importar: {ex.Message}");
        }
        finally
        {
            _loadingService.StopLoading();
        }
    }

    #region Pruebas de Conexión

    [RelayCommand]
    private async Task ProbarWebApiAsync()
    {
        var apiUrl = _configService.GetCurrentApiUrl();
        
        if (string.IsNullOrEmpty(apiUrl))
        {
            await _dialogService.ShowError("No se pudo obtener la URL de la WebAPI.");
            return;
        }

        _loadingService.StartLoading();
        var result = await _connectionTestService.TestWebApiAsync(apiUrl);
        _loadingService.StopLoading();

        await MostrarResultadoPrueba(result);
    }

    [RelayCommand]
    private async Task ProbarFtpAsync()
    {
        _loadingService.StartLoading();
        var result = await _connectionTestService.TestFtpAsync(AppConfig.Global.Ftp);
        _loadingService.StopLoading();

        await MostrarResultadoPrueba(result);
    }

    [RelayCommand]
    private async Task ProbarDvrAsync()
    {
        if (SedeSeleccionada == null) return;

        _loadingService.StartLoading();
        var result = await _connectionTestService.TestDvrAsync(SedeSeleccionada.Dvr);
        _loadingService.StopLoading();

        SedeSeleccionada.Dvr.Conectado = result.Success;
        await MostrarResultadoPrueba(result);
    }

    [RelayCommand]
    private async Task ProbarBalanzaAsync(BalanzaConfig balanza)
    {
        if (balanza == null) return;

        _loadingService.StartLoading();
        var result = await _connectionTestService.TestBalanzaAsync(balanza);
        _loadingService.StopLoading();

        balanza.Conectada = result.Success;
        if (result.AdditionalInfo.ContainsKey("UltimaLectura"))
        {
            balanza.UltimoPeso = result.AdditionalInfo["UltimaLectura"]?.ToString();
        }
        
        await MostrarResultadoPrueba(result);
    }

    private async Task MostrarResultadoPrueba(ConnectionTestResult result)
    {
        if (result.Success)
        {
            await _dialogService.ShowSuccess(result.Message);
        }
        else
        {
            await _dialogService.ShowError(result.Message);
        }
    }

    #endregion

    #region CRUD Sedes

    [RelayCommand]
    private void AgregarSede()
    {
        var nuevaSede = new SedeConfig
        {
            Id = (AppConfig.Sedes.Any() ? AppConfig.Sedes.Max(s => s.Id) : 0) + 1,
            Nombre = "Nueva Sede",
            Codigo = "SEDE-00" + (AppConfig.Sedes.Count + 1),
            Tipo = TipoSede.Pesajes
        };
        AppConfig.Sedes.Add(nuevaSede);
        SedeSeleccionada = nuevaSede;
    }

    [RelayCommand]
    private async Task EliminarSedeAsync()
    {

        if (SedeSeleccionada == null)
        {
            return;
        }
        if (await _dialogService.ShowConfirm("Confirmación", $"¿Eliminar la sede {SedeSeleccionada.Nombre}?"))
        {
            AppConfig.Sedes.Remove(SedeSeleccionada);
            SedeSeleccionada = AppConfig.Sedes.FirstOrDefault();
            
       
            try
            {         
                await _configService.SaveAsync(AppConfig);          
                await _dialogService.ShowSuccess("Sede eliminada correctamente.");
            }
            catch (Exception ex)
            {
               
                await _dialogService.ShowError($"La sede se eliminó pero no se pudo guardar: {ex.Message}");
            }
        }
     
    }

    #endregion

    #region CRUD Balanzas

    [RelayCommand]
    private async Task AgregarBalanzaAsync()
    {
        if (SedeSeleccionada == null) return;

        int maxBalanzas = SedeSeleccionada.GetMaxBalanzas();
        if (SedeSeleccionada.Balanzas.Count >= maxBalanzas)
        {
            await _dialogService.ShowError($"No se pueden agregar más de {maxBalanzas} balanza(s) para una sede de tipo '{SedeSeleccionada.Tipo}'.");
            return;
        }

        var nuevaBalanza = new BalanzaConfig
        {
            Id = (SedeSeleccionada.Balanzas.Any() ? SedeSeleccionada.Balanzas.Max(b => b.Id) : 0) + 1,
            Nombre = $"Balanza {SedeSeleccionada.Balanzas.Count + 1}",
            Puerto = "COM1",
            BaudRate = 9600,
            Activa = true
        };
        SedeSeleccionada.Balanzas.Add(nuevaBalanza);
        BalanzaSeleccionada = nuevaBalanza;
    }

    [RelayCommand]
    private async Task EliminarBalanzaAsync(BalanzaConfig balanza)
    {
        if (SedeSeleccionada == null || balanza == null) return;

        if (await _dialogService.ShowConfirm("Confirmación", $"¿Eliminar la balanza {balanza.Nombre}?"))
        {
            SedeSeleccionada.Balanzas.Remove(balanza);
        }
    }

    #endregion

    #region CRUD Cámaras

    [RelayCommand]
    private void AgregarCamara()
    {
        if (SedeSeleccionada == null) return;

        var nuevaCamara = new CamaraConfig
        {
            Id = (SedeSeleccionada.Camaras.Any() ? SedeSeleccionada.Camaras.Max(c => c.Id) : 0) + 1,
            Nombre = $"Cámara {SedeSeleccionada.Camaras.Count + 1}",
            Canal = SedeSeleccionada.Camaras.Count + 1,
            Activa = true
        };
        SedeSeleccionada.Camaras.Add(nuevaCamara);
        CamaraSeleccionada = nuevaCamara;
    }

    [RelayCommand]
    private async Task EliminarCamaraAsync(CamaraConfig camara)
    {
        if (SedeSeleccionada == null || camara == null) return;

        if (await _dialogService.ShowConfirm("Confirmación", $"¿Eliminar la cámara {camara.Nombre}?"))
        {
            SedeSeleccionada.Camaras.Remove(camara);
        }
    }

    #endregion
}
