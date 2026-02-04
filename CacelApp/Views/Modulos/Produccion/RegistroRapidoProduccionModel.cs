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
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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
    private readonly IProduccionSearchService _produccionSearchService;
    private readonly ISerialPortService _serialPortService;
    private readonly IConfigurationService _configService;

    private readonly Infrastructure.Services.Shared.ISelectOptionService _selectOptionService;
    private readonly CacelApp.Services.ImageAudit.IImageAuditService _imageAuditService;
    
    // Almacén temporal para imágenes capturadas
    private List<MemoryStream> _capturedImages = new();

    #region Propiedades Observables

    [ObservableProperty]
    private ObservableCollection<SelectOption> _materiales = new();

    [ObservableProperty]
    private string? _filtroMaterial;

    public ObservableCollection<SelectOption> MaterialesFiltrados { get; } = new();

    [ObservableProperty]
    private ObservableCollection<SelectOption> _unidadesMedida = new();
    
    [ObservableProperty] 
    private ObservableCollection<SelectOption> maquinaria = new();

    [ObservableProperty]
    private int? _materialSeleccionado;

    [ObservableProperty]
    private string? _materialCodigo;

    [ObservableProperty]
    private string? _materialDescripcion;

    [ObservableProperty]
    private object? _materialExtData;

    [ObservableProperty]
    private int? _unidadMedidaSeleccionada = 49;

    [ObservableProperty]
    private ObservableCollection<BalanzaDisplayInfo> balanzasInfo = new();

    [ObservableProperty] private string? pes_veh_id;

    public BalanzaDisplayInfo? PrimeraBalanza =>
        BalanzasInfo.FirstOrDefault();

    [ObservableProperty]
    private float _pesoBruto ;

    [ObservableProperty]
    private float _pesoTara = 0;

    [ObservableProperty]
    private float _pesoNeto;

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

    public bool CanSave => MaterialSeleccionado.HasValue && UnidadMedidaSeleccionada.HasValue && PesoBruto > 0;

    #endregion

    public RegistroRapidoProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IProduccionService produccionService,
        IProduccionSearchService produccionSearchService,
        ISerialPortService serialPortService,
        IConfigurationService configService,
        Infrastructure.Services.Shared.ISelectOptionService selectOptionService,
        CacelApp.Services.ImageAudit.IImageAuditService imageAuditService)
        : base(dialogService, loadingService)
    {
        _dialogService = dialogService;
        _loadingService = loadingService;
        _produccionService = produccionService;
        _produccionSearchService = produccionSearchService;
        _serialPortService = serialPortService;
        _configService = configService;
        _selectOptionService = selectOptionService;
        _imageAuditService = imageAuditService ?? throw new ArgumentNullException(nameof(imageAuditService));

        // Suscribir eventos de balanza
        _serialPortService.OnPesosLeidos += SerialPortService_OnPesosLeidos;
        _serialPortService.OnEstabilidadCambiada += SerialPortService_OnEstabilidadCambiada;

        _ = InicializarDatosAsync();
    }

    private void SerialPortService_OnPesosLeidos(Dictionary<string, string> lecturas)
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
    }

    private void SerialPortService_OnEstabilidadCambiada(Dictionary<string, bool> estabilidad)
    {
        foreach (var balanza in BalanzasInfo)
        {
            if (estabilidad.TryGetValue(balanza.Puerto, out var estable))
            {
                balanza.EsEstable = estable;
            }
        }
    }

    // Property change handlers
    partial void OnMaterialSeleccionadoChanged(int? value)
    {
        if (value.HasValue)
        {
            var material = Materiales.FirstOrDefault(m => m.Value?.ToString() == value.ToString());
            if (material != null)
            {
                MaterialDescripcion = material.Label;

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

                        MaterialCodigo = materialCodigo;
                        MaterialExtData = material.Ext;
                    }
                    catch { }
                }

                if (value == 6)
                {
                    Pes_veh_id = "C-004";
                }
            }
        }
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

    partial void OnPesoTaraChanged(float value)
    {
        PesoNeto = PesoBruto - PesoTara;
    }
    
    partial void OnPesoBrutoChanged(float value)
    {
        PesoNeto = PesoBruto - PesoTara;
    }

    partial void OnMaterialesChanged(ObservableCollection<SelectOption> value)
    {
        ActualizarMaterialesFiltrados();
    }

    partial void OnFiltroMaterialChanged(string? value)
    {
        ActualizarMaterialesFiltrados();
    }

    private void ActualizarMaterialesFiltrados()
    {
        MaterialesFiltrados.Clear();
        var query = FiltroMaterial?.ToLower() ?? "";
        
        var filtered = string.IsNullOrWhiteSpace(query) 
            ? Materiales 
            : Materiales.Where(m => 
                m.Label.ToLower().Contains(query) || 
                (m.Ext is JsonElement je && je.TryGetProperty("bie_codigo", out var cod) && cod.GetString()?.ToLower().Contains(query) == true)
            );

        foreach (var item in filtered)
        {
            MaterialesFiltrados.Add(item);
        }
        
        IsMaterialListLarge = MaterialesFiltrados.Count > 15;
    }

    [ObservableProperty]
    private bool _isMaterialListLarge;

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

            var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material, null, new { bie_tipo = 3 });
            Materiales.Clear();
            foreach (var m in mats)
            {
                int val = 0;
                if (m.Value is int i) val = i;
                else if (m.Value is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int j)) val = j;

                Materiales.Add(new SelectOption { Value = val, Label = m.Label, Ext = m.Ext });
            }
            IsMaterialListLarge = Materiales.Count > 6;

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

            ActualizarMaterialesFiltrados();
            OnPropertyChanged(nameof(MaterialesFiltrados));

            // Seleccionar default: RESPETAR LA UNIDAD 49
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

    private async Task IniciarLecturaBalanzaAsync()
    {
        try
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
                _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
            }
        }
        catch { }
    }

    private async Task CapturarPesoAsync(string puerto, string nombreBalanza)
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error capturando peso/fotos: {ex.Message}");
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

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            if (!CanSave) return;

            var confirm = await _dialogService.ShowConfirm("¿Desea guardar el registro de producción?", "Confirmar Guardado");
            if (!confirm) return;

            LoadingService?.StartLoading();
            
            var request = new Pde
            {
                pde_bie_id = MaterialSeleccionado.Value,
                pde_bie_cod = MaterialCodigo ?? "",
                pde_bie_des = MaterialDescripcion,
                pde_t6m_id = UnidadMedidaSeleccionada.Value,
                pde_pb = PesoBruto,
                pde_pt = PesoTara,
                pde_pn = PesoNeto,
                pes_veh_id = Pes_veh_id ?? "",
                pes_col_id = Pes_col_id,
                pes_obs = Observaciones,
                pde_nbza = PrimeraBalanza?.Nombre ?? "",
                action = ActionType.Create
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

                await _dialogService.ShowSuccess("Registro guardado correctamente", "Éxito");
                
                if (response.Data != null)
                {
                    try
                    {
                        var pdfData = await _produccionSearchService.GenerateReportPdfAsync(response.Data.pde_id);
                        if (pdfData != null && pdfData.Length > 0)
                        {
                            Application.Current.Dispatcher.Invoke(() => {
                                var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfData, $"Producción - Pesaje {response.Data.pde_pes_des}",true);
                                pdfViewer.Show();
                            });
                        }
                    }
                    catch { }
                }

                Cleanup();
                Application.Current.Dispatcher.Invoke(() => {
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
        Application.Current.Dispatcher.Invoke(() => {
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
