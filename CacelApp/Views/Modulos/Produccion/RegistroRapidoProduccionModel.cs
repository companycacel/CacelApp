using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.WeightDisplay;
using CacelApp.Views.Modulos.Balanza;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Infrastructure.Services.Produccion;
using Services.Shared;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// ViewModel para el registro rápido de producción
/// </summary>
public partial class RegistroRapidoProduccionModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly ILoadingService _loadingService;
    private readonly IProduccionService _produccionService;
    private readonly IFindFileService _findFileService;
    private readonly IProduccionSearchService _produccionSearchService;
    private readonly ISerialPortService _serialPortService;
    private readonly IConfigurationService _configService;

    private readonly Infrastructure.Services.Shared.ISelectOptionService _selectOptionService;
    private readonly CacelApp.Services.ImageAudit.IImageAuditService _imageAuditService;
    private readonly Core.Repositories.Profile.IUserProfileService _userProfileService;

    // Almacén temporal para imágenes capturadas
    private List<MemoryStream> _capturedImages = new();

    #region Propiedades Observables

    [ObservableProperty]
    private ObservableCollection<SelectOption> _materialesCv = new();

    [ObservableProperty]
    private string? _filtroMaterialCv;

    public ObservableCollection<SelectOption> MaterialesCvFiltrados { get; } = new();

    [ObservableProperty]
    private ObservableCollection<SelectOption> _materialesIn = new();

    [ObservableProperty]
    private string? _filtroMaterialIn;

    public ObservableCollection<SelectOption> MaterialesInFiltrados { get; } = new();

    [ObservableProperty]
    private ObservableCollection<SelectOption> _unidadesMedida = new();

    [ObservableProperty]
    private ObservableCollection<SelectOption> maquinaria = new();
    [ObservableProperty]
    private ObservableCollection<SelectOption> motivos = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private int? _materialCvSeleccionado;

    [ObservableProperty]
    private string? _materialCvCodigo;

    [ObservableProperty]
    private string? _materialCvDescripcion;

    [ObservableProperty]
    private object? _materialCvExtData;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private int? _materialInSeleccionado;

    [ObservableProperty]
    private string? _materialInCodigo;

    [ObservableProperty]
    private string? _materialInDescripcion;

    [ObservableProperty]
    private object? _materialInExtData;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private int? _unidadMedidaSeleccionada = 49;

    [ObservableProperty]
    private ObservableCollection<BalanzaDisplayInfo> balanzasInfo = new();

    [ObservableProperty] private string? pes_veh_id;
    [ObservableProperty] private int pes_clase=1;

    public bool EsCvSolo => Pes_clase == 1;
    public bool EsInSolo => Pes_clase == 2;
    public bool EsTransformacion => Pes_clase == 3;

    public BalanzaDisplayInfo? PrimeraBalanza =>
        BalanzasInfo.FirstOrDefault();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private float _pesoBruto;

    [ObservableProperty]
    private int? _pes_col_id;

    [ObservableProperty]
    private ObservableCollection<SelectOption> _responsables = new();

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private string? _observaciones;

    [ObservableProperty]
    private System.Windows.Media.ImageSource? _fotoFrontal;

    [ObservableProperty]
    private System.Windows.Media.ImageSource? _fotoCarga;

    [ObservableProperty]
    private bool _isChecked;
    public bool CanSave => UnidadMedidaSeleccionada.HasValue && 
                           PesoBruto > 0 &&
                           (
                               (Pes_clase == 1 && MaterialCvSeleccionado.HasValue) ||
                               (Pes_clase == 2 && MaterialInSeleccionado.HasValue) ||
                               (Pes_clase == 3 && MaterialCvSeleccionado.HasValue && MaterialInSeleccionado.HasValue)
                           );

    #endregion

    public RegistroRapidoProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IProduccionService produccionService,
        IProduccionSearchService produccionSearchService,
        ISerialPortService serialPortService,
        IConfigurationService configService,
        IFindFileService findFileService,
        Infrastructure.Services.Shared.ISelectOptionService selectOptionService,
        CacelApp.Services.ImageAudit.IImageAuditService imageAuditService,
        Core.Repositories.Profile.IUserProfileService userProfileService)
        : base(dialogService, loadingService)
    {
        _dialogService = dialogService;
        _loadingService = loadingService;
        _produccionService = produccionService;
        _produccionSearchService = produccionSearchService;
        _serialPortService = serialPortService;
        _configService = configService;
        _findFileService = findFileService;
        _selectOptionService = selectOptionService;
        _imageAuditService = imageAuditService ?? throw new ArgumentNullException(nameof(imageAuditService));
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));

        // Suscribir eventos de balanza
        _serialPortService.OnPesosLeidos += SerialPortService_OnPesosLeidos;
        _serialPortService.OnEstabilidadCambiada += SerialPortService_OnEstabilidadCambiada;

        _ = InicializarDatosAsync();
    }

    private void SerialPortService_OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var balanza in BalanzasInfo)
            {
                if (lecturas.TryGetValue(balanza.Puerto, out var pesoStr))
                {
                    if (decimal.TryParse(pesoStr, out var peso))
                    {
                        balanza.PesoActual = peso;
                        balanza.Conectada = true;
                    }
                }
            }
        });
    }

    private void SerialPortService_OnEstabilidadCambiada(Dictionary<string, bool> estabilidad)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var balanza in BalanzasInfo)
            {
                if (estabilidad.TryGetValue(balanza.Puerto, out var estable))
                {
                    balanza.EsEstable = estable;
                }
            }
        });
    }

    // Property change handlers
    partial void OnMaterialCvSeleccionadoChanged(int? value)
    {
        if (value.HasValue)
        {
            var material = MaterialesCv.FirstOrDefault(m => m.Value?.ToString() == value.ToString());
            if (material != null)
            {
                MaterialCvDescripcion = material.Label;

                if (material.Ext != null)
                {
                    try
                    {
                        var extJson = material.Ext?.ToString();
                        string materialCodigo = null;

                        if (!string.IsNullOrWhiteSpace(extJson))
                        {
                            var doc = JsonDocument.Parse(extJson);
                            if (doc.RootElement.TryGetProperty("bie_codigo", out var codigo))
                                materialCodigo = codigo.GetString();
                        }

                        MaterialCvCodigo = materialCodigo;
                        MaterialCvExtData = material.Ext;
                    }
                    catch { }
                }

                if (value == 6)
                {
                    Pes_veh_id = "C-004";
                }
            }
        }
        else
        {
            MaterialCvDescripcion = null;
            MaterialCvCodigo = null;
            MaterialCvExtData = null;
        }

        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnMaterialInSeleccionadoChanged(int? value)
    {
        if (value.HasValue)
        {
            var material = MaterialesIn.FirstOrDefault(m => m.Value?.ToString() == value.ToString());
            if (material != null)
            {
                MaterialInDescripcion = material.Label;

                if (material.Ext != null)
                {
                    try
                    {
                        var extJson = material.Ext?.ToString();
                        string materialCodigo = null;

                        if (!string.IsNullOrWhiteSpace(extJson))
                        {
                            var doc = JsonDocument.Parse(extJson);
                            if (doc.RootElement.TryGetProperty("bie_codigo", out var codigo))
                                materialCodigo = codigo.GetString();
                        }

                        MaterialInCodigo = materialCodigo;
                        MaterialInExtData = material.Ext;
                    }
                    catch { }
                }

                if (value == 6)
                {
                    Pes_veh_id = "C-004";
                }
            }
        }
        else
        {
            MaterialInDescripcion = null;
            MaterialInCodigo = null;
            MaterialInExtData = null;
        }

        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnPes_veh_idChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            IsChecked = false;
        }
    }

    partial void OnIsCheckedChanged(bool value)
    {
        if (value)
        {
            Pes_veh_id = null;
        }
    }
    partial void OnUnidadMedidaSeleccionadaChanged(int? value)
    {
        if (value == 49) { PesoTara = 1; return; }
        if(value== 63) { PesoTara = 2; return; }
        PesoTara = 0;
    }
    [ObservableProperty]
    private float _pesoTara = 1;

    [ObservableProperty]
    private string _pesoTaraInput = "1";

    partial void OnPesoTaraChanged(float value)
    {
        PesoNeto = PesoBruto - PesoTara;
        if (value.ToString() != PesoTaraInput && Math.Abs(float.TryParse(PesoTaraInput, out float current) ? current - value : 1) > 0.001)
        {
             PesoTaraInput = value.ToString();
        }
    }

    partial void OnPesoTaraInputChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            PesoTara = 0;
            return;
        }
        string normalized = value.Replace(",", ".");
        if (float.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            if (Math.Abs(PesoTara - result) > 0.001)
            {
                PesoTara = result;
            }
        }
    }

    [ObservableProperty]
    private float _pesoNeto;

    partial void OnPesoBrutoChanged(float value)
    {
        PesoNeto = PesoBruto - PesoTara;
    }

    partial void OnPes_claseChanged(int value)
    {
        OnPropertyChanged(nameof(EsCvSolo));
        OnPropertyChanged(nameof(EsInSolo));
        OnPropertyChanged(nameof(EsTransformacion));
        OnPropertyChanged(nameof(CanSave));
        GuardarCommand.NotifyCanExecuteChanged();

        _ = CargarMaterialesPorClaseAsync(value);
    }
    partial void OnFiltroMaterialCvChanged(string? value)
    {
        ActualizarMaterialesCvFiltrados();
    }

    partial void OnFiltroMaterialInChanged(string? value)
    {
        ActualizarMaterialesInFiltrados();
    }

    private void ActualizarMaterialesCvFiltrados()
    {
        MaterialesCvFiltrados.Clear();
        var query = FiltroMaterialCv?.ToLower() ?? "";

        var filtered = string.IsNullOrWhiteSpace(query)
            ? MaterialesCv
            : MaterialesCv.Where(m =>
                m.Label.ToLower().Contains(query) ||
                (m.Ext is JsonElement je && je.TryGetProperty("bie_codigo", out var cod) && cod.GetString()?.ToLower().Contains(query) == true)
            );

        foreach (var item in filtered)
        {
            MaterialesCvFiltrados.Add(item);
        }

        IsMaterialCvListLarge = MaterialesCvFiltrados.Count > 15;
    }

    private void ActualizarMaterialesInFiltrados()
    {
        MaterialesInFiltrados.Clear();
        var query = FiltroMaterialIn?.ToLower() ?? "";

        var filtered = string.IsNullOrWhiteSpace(query)
            ? MaterialesIn
            : MaterialesIn.Where(m =>
                m.Label.ToLower().Contains(query) ||
                (m.Ext is JsonElement je && je.TryGetProperty("bie_codigo", out var cod) && cod.GetString()?.ToLower().Contains(query) == true)
            );

        foreach (var item in filtered)
        {
            MaterialesInFiltrados.Add(item);
        }

        IsMaterialInListLarge = MaterialesInFiltrados.Count > 15;
    }

    [ObservableProperty]
    private bool _isMaterialCvListLarge;

    [ObservableProperty]
    private bool _isMaterialInListLarge;

    [ObservableProperty]
    private bool _isMachineryListLarge;
    
    private async Task InicializarDatosAsync()
    {
        try
        {
            LoadingService?.StartLoading();
            await Task.Delay(200);
            var umeds = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Umedida);
            UnidadesMedida.Clear();
            foreach (var u in umeds)
            {
                int val = 0;
                if (u.Value is int i) val = i;
                else if (u.Value is string s && int.TryParse(s, out int p)) val = p;
                else if (u.Value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int j)) val = j;

                UnidadesMedida.Add(new SelectOption { Value = val, Label = u.Label });
            }
            Motivos = new()
            {
                new() { Value = 1, Label = "POR PRODUCTO TERMINADO" },
                new() { Value = 2, Label = "POR MATERIA PRIMA",Disabled=true },
                new() { Value = 3, Label = "CON TRANSFORMACIÓN" }
            };
            await CargarMaterialesPorClaseAsync(Pes_clase);

            var maq = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Maquinaria);
            Maquinaria.Clear();
            foreach (var m in maq)
            {
                Maquinaria.Add(new SelectOption { Value = m.Value, Label = m.Label, Ext = m.Ext });
            }
            IsMachineryListLarge = Maquinaria.Count > 6;

            var resp = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Colaborador);
            Responsables.Clear();
            foreach (var r in resp)
            {
                int val = 0;
                if (r.Value is int i) val = i;
                else if (r.Value is string s && int.TryParse(s, out int p)) val = p;
                else if (r.Value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int j)) val = j;

                Responsables.Add(new SelectOption { Value = val, Label = r.Label });
            }
            if (Responsables.Any()) Pes_col_id = (int)Responsables.First().Value;

            if (!UnidadMedidaSeleccionada.HasValue && UnidadesMedida.Any())
            {
                UnidadMedidaSeleccionada = (int)UnidadesMedida.First().Value;
            }
            else if (UnidadMedidaSeleccionada.HasValue)
            {
                if (!UnidadesMedida.Any(u => u.Value?.ToString() == UnidadMedidaSeleccionada.Value.ToString()))
                {
                    if (UnidadesMedida.Any())
                        UnidadMedidaSeleccionada = (int)UnidadesMedida.First().Value;
                }
            }

            await IniciarLecturaBalanzaAsync();
            PesoNeto = PesoBruto - PesoTara;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error init data: {ex.Message}");
        }
        finally
        {
            LoadingService?.StopLoading();
        }
    }
    private async Task<List<SelectOption>> ObtenerMaterialesAsync(string dpp_tipo)
    {
        var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material, null, new { bie_tipo = 1, _dpp_tipo = dpp_tipo });
        var list = new List<SelectOption>();
        foreach (var m in mats)
        {
            int val = 0;
            if (m.Value is int i) val = i;
            else if (m.Value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int j)) val = j;

            list.Add(new SelectOption { Value = val, Label = m.Label, Ext = m.Ext });
        }
        return list;
    }

    private async Task CargarMaterialesPorClaseAsync(int clase)
    {
        try
        {
            if (clase == 1) // POR PRODUCTO TERMINADO (CV)
            {
                var matsCv = await ObtenerMaterialesAsync("CV");
                MaterialesCv.Clear();
                foreach (var item in matsCv) MaterialesCv.Add(item);
                ActualizarMaterialesCvFiltrados();

                MaterialesIn.Clear();
                MaterialesInFiltrados.Clear();
                MaterialInSeleccionado = null;
                MaterialInDescripcion = null;
                MaterialInCodigo = null;
                MaterialInExtData = null;
            }
            else if (clase == 2) // POR MATERIA PRIMA (IN)
            {
                var matsIn = await ObtenerMaterialesAsync("IN");
                MaterialesIn.Clear();
                foreach (var item in matsIn) MaterialesIn.Add(item);
                ActualizarMaterialesInFiltrados();

                MaterialesCv.Clear();
                MaterialesCvFiltrados.Clear();
                MaterialCvSeleccionado = null;
                MaterialCvDescripcion = null;
                MaterialCvCodigo = null;
                MaterialCvExtData = null;
            }
            else if (clase == 3) // CON TRANSFORMACIÓN (CV e IN)
            {
                var taskCv = ObtenerMaterialesAsync("CV");
                var taskIn = ObtenerMaterialesAsync("IN");
                await Task.WhenAll(taskCv, taskIn);

                MaterialesCv.Clear();
                foreach (var item in await taskCv) MaterialesCv.Add(item);
                ActualizarMaterialesCvFiltrados();

                MaterialesIn.Clear();
                foreach (var item in await taskIn) MaterialesIn.Add(item);
                ActualizarMaterialesInFiltrados();
            }

            if (MaterialCvSeleccionado.HasValue && !MaterialesCv.Any(m => m.Value?.ToString() == MaterialCvSeleccionado.Value.ToString()))
            {
                MaterialCvSeleccionado = null;
                MaterialCvDescripcion = null;
                MaterialCvCodigo = null;
                MaterialCvExtData = null;
            }

            if (MaterialInSeleccionado.HasValue && !MaterialesIn.Any(m => m.Value?.ToString() == MaterialInSeleccionado.Value.ToString()))
            {
                MaterialInSeleccionado = null;
                MaterialInDescripcion = null;
                MaterialInCodigo = null;
                MaterialInExtData = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando materiales: {ex.Message}");
        }
    }
    private async Task IniciarLecturaBalanzaAsync()
    {

        var sede = await _configService.GetSedeActivaAsync();
        if (sede != null && sede.Balanzas.Any())
        {
            BalanzasInfo.Clear();
            foreach (var balanza in sede.Balanzas)
            {
                var nombreBalanza = balanza.Nombre;

                var balanzaInfo = new CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo
                {
                    Nombre = balanza.Nombre,
                    Puerto = balanza.Puerto,
                    ColorBorde = "#10B981",
                    Conectada = balanza.Conectada,
                    MostrarBotonCaptura = true,
                    CapturarCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                            await CapturarPesoAsync(balanza.Puerto, balanza.Nombre)))
                };

                BalanzasInfo.Add(balanzaInfo);
            }

            OnPropertyChanged(nameof(PrimeraBalanza));

            var ultimasLecturas = _serialPortService.ObtenerUltimasLecturas();
            if (ultimasLecturas.Any())
            {
                SerialPortService_OnPesosLeidos(ultimasLecturas);
            }

            var estabilidadActual = _serialPortService.ObtenerEstabilidadActual();
            if (estabilidadActual.Any())
            {
                SerialPortService_OnEstabilidadCambiada(estabilidadActual);
            }

            _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
        }

    }

    private async Task CapturarPesoAsync(string puerto, string nombreBalanza)
    {

        var balanza = BalanzasInfo.FirstOrDefault(b => b.Puerto == puerto);
        if (balanza != null && balanza.PesoActual.HasValue)
        {
            PesoBruto = (float)balanza.PesoActual.Value;
            _capturedImages.Clear();
            var images = await _imageAuditService.CapturarImagenesAsync(nombreBalanza);
            if (images != null && images.Count > 0)
            {
                _capturedImages = images;
                if (images.Count >= 1) FotoFrontal = BitmapFromStream(images[0]);
                if (images.Count >= 2) FotoCarga = BitmapFromStream(images[1]);
            }

            CurrentStep = 2;
        }

    }

    private BitmapImage BitmapFromStream(MemoryStream stream)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(stream.ToArray());
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task GuardarAsync()
    {
        try
        {
            if (!CanSave) return;

            var confirm = await _dialogService.ShowConfirm("¿Desea guardar el registro de producción?", "Confirmar Guardado");
            if (!confirm) return;

            LoadingService?.StartLoading();
            var sesion = await _userProfileService.GetUserProfileAsync();
            int pdeBieId = 0;
            string pdeBieCod = "";
            string? pdeBieDes = null;
            int? bieId = null;

            if (Pes_clase == 1) 
            {
                pdeBieId = MaterialCvSeleccionado!.Value;
                pdeBieCod = MaterialCvCodigo ?? "";
                pdeBieDes = MaterialCvDescripcion;
                bieId = GetValueFromObject<int?>(MaterialCvExtData, "bie_data.bie_id");
            }
            else if (Pes_clase == 2) 
            {
                pdeBieId = MaterialInSeleccionado!.Value;
                pdeBieCod = MaterialInCodigo ?? "";
                pdeBieDes = MaterialInDescripcion;
                bieId = GetValueFromObject<int?>(MaterialInExtData, "bie_data.bie_id");
            }
            else if (Pes_clase == 3) 
            {
                pdeBieId = MaterialCvSeleccionado!.Value;
                pdeBieCod = MaterialCvCodigo ?? "";
                pdeBieDes = MaterialCvDescripcion;
                bieId = MaterialInSeleccionado;
            }
           
            var request = new Pde
            {
                pde_bie_id = pdeBieId,
                pde_bie_cod = pdeBieCod,
                pde_bie_des = pdeBieDes,
                pde_t6m_id = UnidadMedidaSeleccionada.Value,
                pde_pb = PesoBruto,
                pde_pt = PesoTara,
                pde_pn = PesoNeto,
                pes_veh_id = Pes_veh_id ?? "",
                pes_col_id = sesion?.gpe.col?.col_id??Pes_col_id,
                pes_obs = Observaciones,
                pde_nbza = PrimeraBalanza?.Nombre ?? "",
                pde_bie_bie = bieId,
                pes_clase = Pes_clase,
                action = ActionType.Create,
                
            };

            if (_capturedImages.Count > 0)
            {
                request.files = _imageAuditService.ConvertirAFormFiles(_capturedImages);
            }

            var response = await _produccionService.SaveProduccionAsync(request);
            if (response.status == 1)
            {
                if (_capturedImages.Any() && response.Data != null)
                {
                    await _imageAuditService.GuardarImagenesLocalmenteAsync(
                        _capturedImages,
                        response.Data.pde_path,
                        response.Data.pde_media);
                }

                if (response.Data != null)
                {

                    var (pdfData,type )= await _findFileService.FindFile(new
                    {
                        url = "/logistica/produccion",
                        format = FileContentType.GetContentType(FileType.Pdf),
                        action = "I",
                        method = "proPDF",
                        pde_id = response.Data.pde_id
                    });
                    if (pdfData != null && pdfData.Length > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfData, $"Producción - Pesaje {response.Data.pde_pes_des}", true);
                            pdfViewer.Show();
                        });
                    }

                }

                Cleanup();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is RegistroRapidoProduccion) window.Close();
                    }
                });
            }
            else
            {
                await _dialogService.ShowError(response.Meta?.msg ?? "Error al guardar", "Error");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError(ex.Message, "Error");
        }
        finally
        {
            LoadingService?.StopLoading();
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        Cleanup();
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is RegistroRapidoProduccion) window.Close();
            }
        });
    }

    public void Cleanup()
    {
        _serialPortService.OnPesosLeidos -= SerialPortService_OnPesosLeidos;
        _serialPortService.OnEstabilidadCambiada -= SerialPortService_OnEstabilidadCambiada;
        _serialPortService.DetenerLectura();

        foreach (var stream in _capturedImages)
        {
            stream.Dispose();
        }
        _capturedImages.Clear();
    }
}
