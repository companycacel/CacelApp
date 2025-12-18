using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Infrastructure.Services.Shared;
using System.Collections.ObjectModel;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// ViewModel para el mantenimiento de producción
/// Usa tipos de base de datos directamente (Pde)
/// </summary>
public partial class MantProduccionModel : ViewModelBase
{
    private readonly ISelectOptionService _selectOptionService;
    private readonly IConfigurationService _configService;
    private readonly ISerialPortService _serialPortService;
    private readonly ICameraService _cameraService;
    private readonly Infrastructure.Services.Produccion.IProduccionService _produccionService;

    private Pde? _data;
    // Propiedades de Pes (encabezado)
    [ObservableProperty] private DateTime pes_fecha = DateTime.Now;
    [ObservableProperty] private int? pes_col_id;

    // Propiedades de Pde (detalle)
    [ObservableProperty] private int pde_id; // ID del registro (para mostrar en edición)
    [ObservableProperty] private int pde_bie_id;
    [ObservableProperty] private int? pde_t6m_id = 49;
    [ObservableProperty] private string? pde_nbza;
    [ObservableProperty] private string? pde_pb = "0";
    [ObservableProperty] private string? pde_pt = "1";
    [ObservableProperty] private string? pde_pn = "0";
    [ObservableProperty] private string? pde_obs;

    // Colecciones para ComboBox
    [ObservableProperty] private ObservableCollection<SelectOption> materiales = new();
    [ObservableProperty] private ObservableCollection<SelectOption> unidadesMedida = new();
    [ObservableProperty] private ObservableCollection<SelectOption> balanzas = new();
    [ObservableProperty] private ObservableCollection<SelectOption> responsables = new();

    // Propiedades de UI
    [ObservableProperty] private ObservableCollection<CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo> balanzasInfo = new();
    [ObservableProperty] private string? nTicket;
    [ObservableProperty] private bool isPesoBrutoReadOnly = true; // Por defecto, Peso Bruto es readonly

    // Comandos
    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    // Patrón RequestClose para desacoplar del Window
    public Action<bool>? RequestClose { get; set; }

    public MantProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        ISelectOptionService selectOptionService,
        IConfigurationService configService,
        ISerialPortService serialPortService,
        Infrastructure.Services.Produccion.IProduccionService produccionService,
        ProduccionItemDto? item = null,
        ICameraService? cameraService = null) : base(dialogService, loadingService)
    {
        _selectOptionService = selectOptionService;
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _produccionService = produccionService ?? throw new ArgumentNullException(nameof(produccionService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));

        GuardarCommand = SafeCommand(OnGuardarAsync);
        CancelarCommand = new RelayCommand(() => RequestClose?.Invoke(false));

        _ = InicializarCombosAsync(item);
    }

    private async Task InicializarCombosAsync(ProduccionItemDto? item = null)
    {
        try
        {
            LoadingService?.StartLoading();
            var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material, null, new { bie_tipo = 3 });
            Materiales.Clear();
            foreach (var m in mats)
            {
                var valorInt = m.Value is int intVal ? intVal : int.Parse(m.Value?.ToString() ?? "0");
                Materiales.Add(new SelectOption
                {
                    Value = valorInt,
                    Label = m.Label,
                    Ext = m.Ext
                });
            }

            // Unidades de Medida - Asegurar que Value sea int
            var umeds = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Umedida);
            UnidadesMedida.Clear();
            foreach (var u in umeds)
            {
                var valorInt = u.Value is int intVal ? intVal : int.Parse(u.Value?.ToString() ?? "0");
                UnidadesMedida.Add(new SelectOption { Value = valorInt, Label = u.Label });
            }
            // Responsables - Asegurar que Value sea int
            var resp = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Colaborador);
            Responsables.Clear();
            foreach (var r in resp)
            {
                var valorInt = r.Value is int intVal ? intVal : int.Parse(r.Value?.ToString() ?? "0");
                Responsables.Add(new SelectOption { Value = valorInt, Label = r.Label });
            }

            // Balanzas (lista simple de strings)
            Balanzas.Clear();
            var sede = await _configService.GetSedeActivaAsync();
            if (sede != null)
            {
                foreach (var balanza in sede.Balanzas)
                {
                    Balanzas.Add(new SelectOption { Value = balanza.Nombre, Label = balanza.Nombre });
                }
                Balanzas.Add(new SelectOption { Value = "B5-O", Label = "B5-O" });
            }

            // Iniciar lectura de balanzas
            IniciarLecturaBalanzas();

            // Si es edición, setear valores
            if (item != null)
            {
                Pde_id = item.pde_id; // Mostrar número de registro en edición
                NTicket = item.pde_pes_des;
                Pes_fecha = item.pes_fecha;
                Pde_bie_id = item.pde_bie_id;
                Pde_t6m_id = item.pde_t6m_id;
                Pes_col_id = item.pes_col_id;
                Pde_nbza = item.pde_nbza;
                Pde_pb = item.pde_pb.ToString("0.00");
                Pde_pt = item.pde_pt.ToString("0.00");
                Pde_pn = item.pde_pn.ToString("0.00");
                Pde_obs = item.pde_obs;
                _data = item;
            }
            else
            {
                _data = new Pde();
                _data.action = ActionType.Create;
            }
            LoadingService?.StopLoading();

        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar datos: {ex.Message}", "Error");
        }
    }


    /// <summary>
    /// Cálculo automático de peso neto cuando cambia peso bruto
    /// </summary>
    partial void OnPde_pbChanged(string? value)
    {
        CalculateNetWeight();
    }

    /// <summary>
    /// Cálculo automático de peso neto cuando cambia peso tara
    /// </summary>
    partial void OnPde_ptChanged(string? value)
    {
        CalculateNetWeight();
    }

    /// <summary>
    /// Lógica de Peso Tara según Unidad de Medida (basado en CacelTracking)
    /// t6m_id = 49 -> Tara = 1
    /// t6m_id = 63 -> Tara = 2
    /// Otros -> Tara = 0
    /// </summary>
    partial void OnPde_t6m_idChanged(int? value)
    {
        if (value == 49)
        {
            Pde_pt = "1";
        }
        else if (value == 63)
        {
            Pde_pt = "2";
        }
        else if (value.HasValue)
        {
            Pde_pt = "0";
        }
    }

    /// <summary>
    /// Resetear tara cuando cambia la balanza y controlar editabilidad de Peso Bruto
    /// </summary>
    partial void OnPde_nbzaChanged(string? value)
    {
        IsPesoBrutoReadOnly = value != "B5-O";
        Pde_pt = "0";
        if (!IsPesoBrutoReadOnly)
        {
            Pde_pb = "0";
        }
    }

    /// <summary>
    /// Calcula el peso neto automáticamente (Bruto - Tara)
    /// </summary>
    private void CalculateNetWeight()
    {
        decimal pb = 0;
        decimal pt = 0;

        decimal.TryParse(Pde_pb, out pb);
        decimal.TryParse(Pde_pt, out pt);

        // Validar que la tara no supere el peso bruto
        if (pt > pb && pb > 0)
        {
            Pde_pt = "0";
            pt = 0;
        }

        Pde_pn = (pb - pt).ToString("0.00");
    }



    // Imágenes capturadas temporalmente (en memoria)
    public List<System.IO.MemoryStream> ImagenesCapturadas { get; private set; } = new();

    private async Task OnGuardarAsync()
    {
        // Validación básica
        if (Pde_bie_id <= 0 || Pde_t6m_id == null || Pes_col_id == null ||
            string.IsNullOrWhiteSpace(Pde_pb) || string.IsNullOrWhiteSpace(Pde_pt) || string.IsNullOrWhiteSpace(Pde_nbza))
        {
            await DialogService.ShowWarning("Complete todos los campos obligatorios.", "Validación");
            return;
        }

        // Validar que los pesos sean números válidos
        if (!decimal.TryParse(Pde_pb, out decimal pb) || pb <= 0)
        {
            await DialogService.ShowWarning("El peso bruto debe ser un número válido mayor a 0.", "Validación");
            return;
        }

        if (!decimal.TryParse(Pde_pt, out decimal pt) || pt < 0)
        {
            await DialogService.ShowWarning("El peso tara debe ser un número válido mayor o igual a 0.", "Validación");
            return;
        }

        _data.pes_fecha = Pes_fecha;
        _data.pes_col_id = Pes_col_id;
        _data.pde_bie_id = Pde_bie_id;
        _data.pde_t6m_id = Pde_t6m_id;
        _data.pde_nbza = Pde_nbza;
        _data.pde_pb = float.TryParse(Pde_pb, out float pbFloat) ? pbFloat : 0;
        _data.pde_pt = float.TryParse(Pde_pt, out float ptFloat) ? ptFloat : 0;
        _data.pde_pn = float.TryParse(Pde_pn, out float pnFloat) ? pnFloat : 0;
        _data.pde_obs = Pde_obs;
        _data.files = ImagenesCapturadas.Select((ms, index) =>
        {
            var bytes = ms.ToArray();
            return (Microsoft.AspNetCore.Http.IFormFile)new SimpleFormFile(bytes, "files", $"{index + 1}.jpg");
        }).ToList();


        var response = await _produccionService.SaveProduccionAsync(_data);
        _data = response.Data;

        await DialogService.ShowSuccess(response.Meta.msg, "Éxito");
        RequestClose?.Invoke(true);
    }

    private async Task CapturarPesoBalanzaAsync(string nombreBalanza)
    {
        var balanza = BalanzasInfo.FirstOrDefault(b => b.Nombre == nombreBalanza);
        if (balanza == null || !balanza.PesoActual.HasValue) return;

        Pde_pb = balanza.PesoActual.Value.ToString("0.00");
        Pde_nbza = nombreBalanza;
        await CapturarFotosCamarasAsync();
    }

    private async Task CapturarFotosCamarasAsync()
    {
        try
        {
            if (Pde_nbza == "B5-O")
            {
                return;
            }

            // Limpiar memoria de imágenes anteriores antes de capturar nuevas
            if (ImagenesCapturadas != null && ImagenesCapturadas.Any())
            {
                foreach (var stream in ImagenesCapturadas)
                {
                    stream?.Dispose();
                }
                ImagenesCapturadas.Clear();
            }
            var sede = await _configService.GetSedeActivaAsync();
            if (sede == null || !sede.RequiereCamaras()) return;
            var balanzaConfig = sede.Balanzas.FirstOrDefault(b => b.Nombre == Pde_nbza);
            if (balanzaConfig == null || !balanzaConfig.CanalesCamaras.Any()) return;
            var estadoCamaras = _cameraService.ObtenerEstadoCamaras();
            if (!estadoCamaras.Any())
            {
                if (!await _cameraService.InicializarAsync(sede.Dvr, sede.Camaras.ToList()))
                {
                    return;
                }
            }
            foreach (var canal in balanzaConfig.CanalesCamaras)
            {
                if (!estadoCamaras.ContainsKey(canal) || !estadoCamaras[canal])
                {
                    _cameraService.IniciarStreaming(canal, IntPtr.Zero);
                }
            }

            foreach (var canal in balanzaConfig.CanalesCamaras)
            {
                try
                {
                    var imagenStream = await _cameraService.CapturarImagenAsync(canal);
                    if (imagenStream != null)
                    {
                        ImagenesCapturadas.Add(imagenStream);
                    }
                }
                catch
                {
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error capturando fotos: {ex.Message}");
        }
    }
    private Dictionary<string, string> _balanzaPuertoMap = new();

    private async void IniciarLecturaBalanzas()
    {
        var sede = await _configService.GetSedeActivaAsync();
        if (sede != null && sede.Balanzas.Any())
        {
            BalanzasInfo.Clear();
            _balanzaPuertoMap.Clear();

            var colores = new[] { "#4F46E5", "#10B981", "#F59E0B", "#EF4444" };
            int colorIndex = 0;

            foreach (var balanza in sede.Balanzas)
            {
                if (!string.IsNullOrEmpty(balanza.Puerto))
                {
                    _balanzaPuertoMap[balanza.Puerto] = balanza.Nombre;
                }

                // Capturar el nombre de la balanza en una variable local para evitar closure issues
                var nombreBalanza = balanza.Nombre;

                var balanzaInfo = new CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo
                {
                    Nombre = balanza.Nombre,
                    Puerto = balanza.Puerto,
                    ColorBorde = colores[colorIndex % colores.Length],
                    Conectada = balanza.Conectada,
                    MostrarBotonCaptura = true,
                    CapturarCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                            await CapturarPesoBalanzaAsync(nombreBalanza)))
                };

                BalanzasInfo.Add(balanzaInfo);
                colorIndex++;
            }

            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            _serialPortService.OnEstabilidadCambiada += OnEstabilidadCambiada;
            
            var ultimasLecturas = _serialPortService.ObtenerUltimasLecturas();
            if (ultimasLecturas.Any())
            {
                OnPesosLeidos(ultimasLecturas);
            }

            // Inicializar estado de estabilidad
            var estabilidadActual = _serialPortService.ObtenerEstabilidadActual();
            if (estabilidadActual.Any())
            {
                OnEstabilidadCambiada(estabilidadActual);
            }

            _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
        }
    }

    private void OnEstabilidadCambiada(Dictionary<string, bool> estabilidades)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var estabilidad in estabilidades)
            {
                var balanzaInfo = BalanzasInfo.FirstOrDefault(b => b.Puerto == estabilidad.Key);
                if (balanzaInfo != null)
                {
                    balanzaInfo.EsEstable = estabilidad.Value;
                }
            }
        });
    }

    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var lectura in lecturas)
            {
                var balanzaInfo = BalanzasInfo.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanzaInfo != null && decimal.TryParse(lectura.Value, out decimal peso))
                {
                    balanzaInfo.PesoActual = peso;
                    balanzaInfo.Conectada = true;
                }
            }
        });
    }

    public void Cleanup()
    {
        _serialPortService.DetenerLectura();
        _serialPortService.OnPesosLeidos -= OnPesosLeidos;
        _serialPortService.OnEstabilidadCambiada -= OnEstabilidadCambiada;
    }
}
