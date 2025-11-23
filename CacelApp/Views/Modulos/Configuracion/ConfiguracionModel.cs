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

    [ObservableProperty]
    private BalanzaConfig? _balanzaSeleccionada;

    [ObservableProperty]
    private CamaraConfig? _camaraSeleccionada;

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
            _loadingService.StartLoading();
            AppConfig = await _configService.LoadAsync();
            
            if (AppConfig.Sedes.Any())
            {
                SedeSeleccionada = AppConfig.GetSedeActiva() ?? AppConfig.Sedes.First();
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
                await _dialogService.ShowError("La configuración de la sede actual no es válida. Verifique que no haya más de 2 balanzas.");
                return;
            }

            await _configService.SaveAsync(AppConfig);
            await _dialogService.ShowSuccess("Configuración guardada exitosamente.");
            
            // Reiniciar servicios si es necesario
            // _serialPortService.Reiniciar();
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
                    AppConfig = await _configService.ImportAsync(openFileDialog.FileName);
                    
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
        if (string.IsNullOrEmpty(AppConfig.Global.WebApiUrl))
        {
            await _dialogService.ShowError("Configure la URL de la WebAPI primero.");
            return;
        }

        _loadingService.StartLoading();
        var result = await _connectionTestService.TestWebApiAsync(AppConfig.Global.WebApiUrl);
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
        if (SedeSeleccionada == null) return;

        if (await _dialogService.ShowConfirm("Confirmación", $"¿Eliminar la sede {SedeSeleccionada.Nombre}?"))
        {
            AppConfig.Sedes.Remove(SedeSeleccionada);
            SedeSeleccionada = AppConfig.Sedes.FirstOrDefault();
        }
    }

    #endregion

    #region CRUD Balanzas

    [RelayCommand]
    private async Task AgregarBalanzaAsync()
    {
        if (SedeSeleccionada == null) return;

        if (SedeSeleccionada.Balanzas.Count >= 2)
        {
            await _dialogService.ShowError("No se pueden agregar más de 2 balanzas por sede.");
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
