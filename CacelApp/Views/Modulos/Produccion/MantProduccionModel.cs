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
    private readonly CacelApp.Services.ImageAudit.IImageAuditService _imageAuditService;

    private Pde? _data;
    [ObservableProperty] private DateTime pes_fecha = DateTime.Now;
    [ObservableProperty] private int? pes_col_id;
    [ObservableProperty] private int? pes_clase = 1;

    // Propiedades de Pde (detalle)
    [ObservableProperty] private int pde_id; 
    [ObservableProperty] private int pde_bie_id;
    [ObservableProperty] private int? pde_bie_bie;
    [ObservableProperty] private int? pde_t6m_id = 49;
    [ObservableProperty] private string? pde_nbza;
    [ObservableProperty] private string? pde_pb = "0";
    [ObservableProperty] private string? pde_pt = "1";
    [ObservableProperty] private string? pde_pn = "0";
    [ObservableProperty] private string? pde_obs;
    [ObservableProperty] private string? pes_veh_id;

    [ObservableProperty] private object? materialCvExtData;
    [ObservableProperty] private object? materialInExtData;

    // Colecciones para ComboBox
    [ObservableProperty] private ObservableCollection<SelectOption> motivos = new();
    [ObservableProperty] private ObservableCollection<SelectOption> materialesCv = new();
    [ObservableProperty] private ObservableCollection<SelectOption> materialesIn = new();
    [ObservableProperty] private ObservableCollection<SelectOption> unidadesMedida = new();
    [ObservableProperty] private ObservableCollection<SelectOption> balanzas = new();
    [ObservableProperty] private ObservableCollection<SelectOption> responsables = new();
    [ObservableProperty] private ObservableCollection<SelectOption> maquinaria = new();

    public bool EsCvSolo => Pes_clase == 1;
    public bool EsInSolo => Pes_clase == 2;
    public bool EsTransformacion => Pes_clase == 3;

    // Propiedades de UI
    [ObservableProperty] private ObservableCollection<CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo> balanzasInfo = new();
    [ObservableProperty] private string? nTicket;
    [ObservableProperty] private bool isPesoBrutoReadOnly = true; 


    public IAsyncRelayCommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }
    public IRelayCommand ReiniciarSerialCommand { get; }
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private bool isSedeC;

    public Action<bool>? RequestClose { get; set; }

    public MantProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        ISelectOptionService selectOptionService,
        IConfigurationService configService,
        ISerialPortService serialPortService,
        Infrastructure.Services.Produccion.IProduccionService produccionService,
        CacelApp.Services.ImageAudit.IImageAuditService imageAuditService,
        ProduccionItemDto? item = null,
        ICameraService? cameraService = null) : base(dialogService, loadingService)
    {
        _selectOptionService = selectOptionService;
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _produccionService = produccionService ?? throw new ArgumentNullException(nameof(produccionService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _imageAuditService = imageAuditService ?? throw new ArgumentNullException(nameof(imageAuditService));

        _imageAuditService = imageAuditService ?? throw new ArgumentNullException(nameof(imageAuditService));

        GuardarCommand = new AsyncRelayCommand(
            async () => await ExecuteSafeAsync(OnGuardarAsync),
            () => !IsSedeC);

        CancelarCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        ReiniciarSerialCommand = new RelayCommand(() =>
        {
            Cleanup();
            IniciarLecturaBalanzas();
        });

        _ = InicializarCombosAsync(item);
    }

    private async Task<List<SelectOption>> ObtenerMaterialesAsync(string dpp_tipo)
    {
        var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material, null, new { bie_tipo = 1, _dpp_tipo = dpp_tipo });
        var list = new List<SelectOption>();
        foreach (var m in mats)
        {
            var valorInt = m.Value is int intVal ? intVal : int.Parse(m.Value?.ToString() ?? "0");
            list.Add(new SelectOption
            {
                Value = valorInt,
                Label = m.Label,
                Ext = m.Ext
            });
        }
        return list;
    }

    private async Task CargarMaterialesPorClaseAsync(int? clase)
    {
        try
        {
            if (clase == 1) // POR PRODUCTO TERMINADO (CV)
            {
                var matsCv = await ObtenerMaterialesAsync("CV");
                MaterialesCv.Clear();
                foreach (var item in matsCv) MaterialesCv.Add(item);

                MaterialesIn.Clear();
                Pde_bie_bie = null;
                MaterialInExtData = null;
            }
            else if (clase == 2) // POR MATERIA PRIMA (IN)
            {
                var matsIn = await ObtenerMaterialesAsync("IN");
                MaterialesIn.Clear();
                foreach (var item in matsIn) MaterialesIn.Add(item);

                MaterialesCv.Clear();
                Pde_bie_bie = null;
                MaterialCvExtData = null;
            }
            else if (clase == 3) // CON TRANSFORMACIÓN (CV e IN)
            {
                var taskCv = ObtenerMaterialesAsync("CV");
                var taskIn = ObtenerMaterialesAsync("IN");
                await Task.WhenAll(taskCv, taskIn);

                MaterialesCv.Clear();
                foreach (var item in await taskCv) MaterialesCv.Add(item);

                MaterialesIn.Clear();
                foreach (var item in await taskIn) MaterialesIn.Add(item);
            }

            if (Pde_bie_id > 0)
            {
                var currentList = (clase == 2) ? MaterialesIn : MaterialesCv;
                if (!currentList.Any(m => (int)(m.Value ?? 0) == Pde_bie_id))
                {
                    Pde_bie_id = 0;
                    MaterialCvExtData = null;
                    MaterialInExtData = null;
                }
            }

            if (Pde_bie_bie.HasValue && Pde_bie_bie > 0)
            {
                if (!MaterialesIn.Any(m => (int)(m.Value ?? 0) == Pde_bie_bie.Value))
                {
                    Pde_bie_bie = null;
                    MaterialInExtData = null;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando materiales: {ex.Message}");
        }
    }

    partial void OnPes_claseChanged(int? value)
    {
        OnPropertyChanged(nameof(EsCvSolo));
        OnPropertyChanged(nameof(EsInSolo));
        OnPropertyChanged(nameof(EsTransformacion));

        _ = CargarMaterialesPorClaseAsync(value);
    }

    private async Task InicializarCombosAsync(ProduccionItemDto? item = null)
    {
        try
        {
            Cleanup();
            LoadingService?.StartLoading();

            Motivos = new()
            {
                new() { Value = 1, Label = "POR PRODUCTO TERMINADO" },
                new() { Value = 2, Label = "POR MATERIA PRIMA" },
                new() { Value = 3, Label = "CON TRANSFORMACIÓN" }
            };

            var umeds = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Umedida);
            UnidadesMedida.Clear();
            foreach (var u in umeds)
            {
                var valorInt = u.Value is int intVal ? intVal : int.Parse(u.Value?.ToString() ?? "0");
                UnidadesMedida.Add(new SelectOption { Value = valorInt, Label = u.Label });
            }
    
            var resp = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Colaborador);
            Responsables.Clear();
            foreach (var r in resp)
            {
                var valorInt = r.Value is int intVal ? intVal : int.Parse(r.Value?.ToString() ?? "0");
                Responsables.Add(new SelectOption { Value = valorInt, Label = r.Label });
            }


            Balanzas.Clear();
            var sede = await _configService.GetSedeActivaAsync();
            if (sede != null)
            {
                foreach (var balanza in sede.Balanzas)
                {
                    Balanzas.Add(new SelectOption { Value = balanza.Nombre, Label = balanza.Nombre });
                }
                Balanzas.Add(new SelectOption { Value = "B0-O", Label = "B0-O" });
                IsSedeC = sede.Balanzas?.FirstOrDefault()?.Nombre.Contains("-C")??false;
            }

            var maquinaria = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Maquinaria);
            Maquinaria.Clear();
            Maquinaria.Add(new SelectOption { Value = "", Label = "SELECCIONE" });
            foreach (var m in maquinaria)
            {
                Maquinaria.Add(new SelectOption
                {
                    Value = m.Value,
                    Label = m.Label,
                    Ext = m.Ext
                });
            }
           
            IniciarLecturaBalanzas();

            if (item != null)
            {
                Pes_clase = item.pes_clase ;
                await CargarMaterialesPorClaseAsync(Pes_clase);

                Pde_id = item.pde_id; 
                NTicket = item.pde_pes_des;
                Pes_fecha = item.pes_fecha;
                Pde_bie_id = item.pde_bie_id;
                Pde_bie_bie = item.pde_bie_bie;
                Pde_t6m_id = item.pde_t6m_id;
                Pes_col_id = item.pes_col_id;
                Pde_nbza = item.pde_nbza;
                Pde_pb = item.pde_pb.ToString("0.00");
                Pde_pt = item.pde_pt.ToString("0.00");
                Pde_pn = item.pde_pn.ToString("0.00");
                Pde_obs = item.pde_obs;
                Pes_veh_id = item.pes_veh_id;
                _data = item;
            }
            else
            {
                Pes_clase = 1;
                await CargarMaterialesPorClaseAsync(Pes_clase);

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

    partial void OnPde_pbChanged(string? value)
    {
        CalculateNetWeight();
    }

    partial void OnPde_ptChanged(string? value)
    {
        CalculateNetWeight();
    }

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

    partial void OnPde_bie_idChanged(int value)
    {
        if(value == 6)
        {
            Pes_veh_id = "C-004";
        }
    }

    partial void OnPde_bie_bieChanged(int? value)
    {
        if(value == 6)
        {
            Pes_veh_id = "C-004";
        }
    }

    /// <summary>
    /// Resetear tara cuando cambia la balanza y controlar editabilidad de Peso Bruto
    /// </summary>
    partial void OnPde_nbzaChanged(string? value)
    {
        IsPesoBrutoReadOnly = value != "B0-O";
        Pde_pt = "0";
        if (!IsPesoBrutoReadOnly)
        {
            Pde_pb = "0";
        }
    }

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


    public List<System.IO.MemoryStream> ImagenesCapturadas { get; private set; } = new();

    private async Task OnGuardarAsync()
    {
        if (Pde_bie_id <= 0 || (Pes_clase == 3 && (!Pde_bie_bie.HasValue || Pde_bie_bie <= 0)) || Pde_t6m_id == null || Pes_col_id == null  ||
        string.IsNullOrWhiteSpace(Pde_pb) || string.IsNullOrWhiteSpace(Pde_pt) || string.IsNullOrWhiteSpace(Pde_nbza))
        {
            await DialogService.ShowWarning("Complete todos los campos obligatorios.", "Validación");
            return;
        }

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

        int? bie_id = null;
        if (Pes_clase == 1)
        {
            bie_id = GetValueFromObject<int?>(MaterialCvExtData, "bie_data.bie_id"); ;
        }
        else if (Pes_clase == 2)
        {
            bie_id = GetValueFromObject<int?>(MaterialInExtData, "bie_data.bie_id");
        }
        else if (Pes_clase == 3)
        {
            bie_id = Pde_bie_bie;
        }

        _data.pes_fecha = Pes_fecha;
        _data.pes_col_id = Pes_col_id;
        _data.pes_clase = Pes_clase;
        _data.pde_bie_id = Pde_bie_id;
        _data.pde_t6m_id = Pde_t6m_id;
        _data.pde_nbza = Pde_nbza;
        _data.pde_pb = float.TryParse(Pde_pb, out float pbFloat) ? pbFloat : 0;
        _data.pde_pt = float.TryParse(Pde_pt, out float ptFloat) ? ptFloat : 0;
        _data.pde_pn = float.TryParse(Pde_pn, out float pnFloat) ? pnFloat : 0;
        _data.pde_obs = Pde_obs;
        _data.pes_veh_id = Pes_veh_id;
        _data.files = _imageAuditService.ConvertirAFormFiles(ImagenesCapturadas);
        _data.pde_bie_bie = bie_id;
        var response = await _produccionService.SaveProduccionAsync(_data);
        _data = response.Data;

        if (_data.action == ActionType.Create && ImagenesCapturadas.Any())
        {
            await _imageAuditService.GuardarImagenesLocalmenteAsync(
                ImagenesCapturadas,
                response.Data.pde_path,
                response.Data.pde_media);
        }

        await DialogService.ShowSuccess(response.Meta.msg, "Éxito");
        RequestClose?.Invoke(true);
    }

    private async Task CapturarPesoBalanzaAsync(string nombreBalanza)
    {
        var balanza = BalanzasInfo.FirstOrDefault(b => b.Nombre == nombreBalanza);
        if (balanza == null || !balanza.PesoActual.HasValue) return;

        Pde_pb = balanza.PesoActual.Value.ToString("0.00");
        Pde_nbza = nombreBalanza;

        if (ImagenesCapturadas != null && ImagenesCapturadas.Any())
        {
            foreach (var stream in ImagenesCapturadas)
            {
                stream?.Dispose();
            }
            ImagenesCapturadas.Clear();
        }

        ImagenesCapturadas = await _imageAuditService.CapturarImagenesAsync(nombreBalanza);
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
