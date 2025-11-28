using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CacelApp.Views.Modulos.Balanza;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Produccion;
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
    private readonly IProduccionService _produccionService;

    private Pde? _data;
    // Propiedades de Pes (encabezado)
    [ObservableProperty] private DateTime pes_fecha = DateTime.Now;
    [ObservableProperty] private int? pes_col_id;

    // Propiedades de Pde (detalle)
    [ObservableProperty] private int pde_bie_id;
    [ObservableProperty] private int? pde_t6m_id;
    [ObservableProperty] private string? pde_nbza;
    [ObservableProperty] private float pde_pb;
    [ObservableProperty] private float pde_pt;
    [ObservableProperty] private float pde_pn;
    [ObservableProperty] private string? pde_obs;

    // Colecciones para ComboBox
    [ObservableProperty] private ObservableCollection<SelectOption> materiales = new();
    [ObservableProperty] private ObservableCollection<SelectOption> unidadesMedida = new();
    [ObservableProperty] private ObservableCollection<SelectOption> balanzas = new();
    [ObservableProperty] private ObservableCollection<SelectOption> responsables = new();

    // Propiedades de UI
    [ObservableProperty] private float? pesoB1;
    [ObservableProperty] private float? pesoB2;
    [ObservableProperty] private string? nTicket;

    // Comandos
    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand CapturarB1Command { get; }
    public ICommand CapturarB2Command { get; }

    // Patrón RequestClose para desacoplar del Window
    public Action? RequestClose { get; set; }

    public MantProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        ISelectOptionService selectOptionService,
        IConfigurationService configService,
        ISerialPortService serialPortService,
        IProduccionService produccionService,
        ProduccionItemDto? item = null,
        ICameraService? cameraService = null) : base(dialogService, loadingService)
    {
        _selectOptionService = selectOptionService;
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _produccionService = produccionService ?? throw new ArgumentNullException(nameof(produccionService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));

        GuardarCommand = SafeCommand(OnGuardarAsync);
        CancelarCommand = new RelayCommand(() => RequestClose?.Invoke());
        CapturarB1Command = SafeCommand(CapturarB1Async);
        CapturarB2Command = SafeCommand(CapturarB2Async);

        _ = InicializarCombosAsync(item);
    }

    private async Task InicializarCombosAsync(ProduccionItemDto? item = null)
    {
        try
        {
            // Materiales - Asegurar que Value sea int y preservar Ext
            var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material);
            Materiales.Clear();
            foreach (var m in mats)
            {
                var valorInt = m.Value is int intVal ? intVal : int.Parse(m.Value?.ToString() ?? "0");
                Materiales.Add(new SelectOption 
                { 
                    Value = valorInt, 
                    Label = m.Label,
                    Ext = m.Ext  // ✅ Preservar datos adicionales (bie_t6m_id)
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
                Pes_fecha = item.pes_fecha;
                Pde_bie_id = item.pde_bie_id;
                Pde_t6m_id = item.pde_t6m_id;
                Pes_col_id = item.pes_col_id;
                Pde_nbza = item.pde_nbza;
                Pde_pb = item.pde_pb;
                Pde_pt = item.pde_pt;
                Pde_pn = item.pde_pn;
                Pde_obs = item.pde_obs;
                _data = item;
            }
            else
            {
                _data = new Pde();
                _data.action=ActionType.Create;
            }
            
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar datos: {ex.Message}", "Error");
        }
    }

    /// <summary>
    /// Cálculo automático de peso neto cuando cambia peso bruto
    /// </summary>
    partial void OnPde_pbChanged(float value)
    {
        Pde_pn = value - Pde_pt;
    }

    /// <summary>
    /// Cálculo automático de peso neto cuando cambia peso tara
    /// </summary>
    partial void OnPde_ptChanged(float value)
    {
        Pde_pn = Pde_pb - value;
    }


    // Imágenes capturadas temporalmente (en memoria)
    public List<System.IO.MemoryStream> ImagenesCapturadas { get; private set; } = new();

    private async Task OnGuardarAsync()
    {
        // Validación básica
        if (Pde_bie_id <= 0 || Pde_t6m_id == null || Pes_col_id == null ||
            Pde_pb <= 0 || Pde_pt < 0 || string.IsNullOrWhiteSpace(Pde_nbza))
        {
            await DialogService.ShowWarning("Complete todos los campos obligatorios.", "Validación");
            return;
        }

        try
        {
           
            _data.pes_fecha = Pes_fecha;
            _data.pes_col_id = Pes_col_id;
            _data.pde_bie_id = Pde_bie_id;
            _data.pde_t6m_id = Pde_t6m_id;
            _data.pde_nbza = Pde_nbza;
            _data.pde_pb = Pde_pb;
            _data.pde_pt = Pde_pt;
            _data.pde_pn = Pde_pn;
            _data.pde_obs = Pde_obs;
            _data.files = ImagenesCapturadas.Select((ms, index) =>
            {
                var bytes = ms.ToArray();
                return (Microsoft.AspNetCore.Http.IFormFile)new SimpleFormFile(bytes, "files", $"{index + 1}.jpg");
            }).ToList();


            var response = await _produccionService.Produccion(_data);
            _data = response.Data;

            await DialogService.ShowSuccess(response.Meta.msg, "Éxito");
            RequestClose?.Invoke();


        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al guardar: {ex.Message}", "Error");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    private async Task CapturarB1Async()
    {

        Pde_pb = PesoB1 ?? 0;
        Pde_nbza = "B1-A";
        await CapturarFotosCamarasAsync();

    }

    private async Task CapturarB2Async()
    {

        Pde_pb = PesoB2 ?? 0;
        Pde_nbza = "B2-A";
        await CapturarFotosCamarasAsync();

    }

    private async Task CapturarFotosCamarasAsync()
    {
        try
        {
            ImagenesCapturadas.Clear();

            // 1. Obtener configuración de la sede activa
            var sede = await _configService.GetSedeActivaAsync();
            if (sede == null || !sede.RequiereCamaras()) return;

            // 2. Obtener la balanza activa (asumimos la primera por ahora o la que coincida con el nombre si tuviéramos esa info)
            var balanzaConfig = sede.Balanzas.FirstOrDefault(b => b.Activa);
            if (balanzaConfig == null || !balanzaConfig.CanalesCamaras.Any()) return;

            // 3. Inicializar servicio de cámaras si es necesario
            var estadoCamaras = _cameraService.ObtenerEstadoCamaras();
            if (!estadoCamaras.Any())
            {
                // Primera vez, inicializar
                if (!await _cameraService.InicializarAsync(sede.Dvr, sede.Camaras.ToList()))
                {
                    return;
                }

                // Iniciar streaming invisible para los canales necesarios
                foreach (var canal in balanzaConfig.CanalesCamaras)
                {
                    _cameraService.IniciarStreaming(canal, IntPtr.Zero);
                }
            }

            // 4. Capturar imágenes de los canales asociados
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
                    // Ignorar errores individuales
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error capturando fotos: {ex.Message}");
        }
    }
    private async void IniciarLecturaBalanzas()
    {
        var sede = await _configService.GetSedeActivaAsync();
        if (sede != null && sede.Balanzas.Any())
        {
            // Iniciar servicio
            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
        }
    }

    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        // Actualizar propiedades en el hilo de la UI
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var sede = await _configService.GetSedeActivaAsync();
            if (sede == null) return;

            foreach (var lectura in lecturas)
            {
                // Buscar qué balanza es por el puerto
                var balanza = sede.Balanzas.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanza != null)
                {
                    if (float.TryParse(lectura.Value, out float peso))
                    {
                        if (sede.Balanzas.Count > 0 && balanza.Id == sede.Balanzas[0].Id) PesoB1 = peso;
                        if (sede.Balanzas.Count > 1 && balanza.Id == sede.Balanzas[1].Id) PesoB2 = peso;
                    }
                }
            }
        });
    }

    public void Cleanup()
    {
        _serialPortService.DetenerLectura();
        _serialPortService.OnPesosLeidos -= OnPesosLeidos;
    }
}
