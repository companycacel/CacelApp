using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Infrastructure.Services.Produccion;
using System.Collections.ObjectModel;
using System.Text.Json;
using Application = System.Windows.Application;
using Window = System.Windows.Window;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// ViewModel para el registro rápido de producción
/// Optimizado para entrada rápida con teclado y balanza
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
    private ObservableCollection<CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo> balanzasInfo = new();

    [ObservableProperty] private string? pes_veh_id;
    public CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo? PrimeraBalanza =>
        BalanzasInfo.FirstOrDefault();

    [ObservableProperty]
    private float _pesoBruto;

    [ObservableProperty]
    private float _pesoTara;

    [ObservableProperty]
    private float _pesoNeto;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private string? _observaciones;

    [ObservableProperty]
    private System.Windows.Media.ImageSource? _fotoFrontal;

    [ObservableProperty]
    private System.Windows.Media.ImageSource? _fotoCarga;

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

        _ = InicializarDatosAsync();
        IniciarLecturaBalanza();
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

    partial void OnPesoTaraChanged(float value)
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

            // 1. Cargar Unidades
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

            // 2. Cargar Materiales
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

            // 3. Cargar Maquinaria
            var maq = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Maquinaria);
            Maquinaria.Clear();
            foreach (var m in maq)
            {
                 Maquinaria.Add(new SelectOption { Value = m.Value, Label = m.Label, Ext = m.Ext });
            }
            IsMachineryListLarge = Maquinaria.Count > 6;

            // Asegurar actualización de listas filtradas
            ActualizarMaterialesFiltrados();
            OnPropertyChanged(nameof(MaterialesFiltrados));

            // Seleccionar default
            if (UnidadesMedida.Any())
                UnidadMedidaSeleccionada = (int)UnidadesMedida.First().Value;

            await IniciarLecturaBalanza();
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

    private async Task IniciarLecturaBalanza()
    {
        try
        {
            Cleanup();
            var sede = await _configService.GetSedeActivaAsync();
            if (sede != null && sede.Balanzas.Any())
            {
                BalanzasInfo.Clear();
                var balanza = sede.Balanzas.First();

                var balanzaInfo = new CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo
                {
                    Nombre = balanza.Nombre,
                    Puerto = balanza.Puerto,
                    Conectada = balanza.Conectada,
                    MostrarBotonCaptura = true,
                    CapturarCommand = new RelayCommand(async () =>
                    {
                        var bal = BalanzasInfo.FirstOrDefault();
                        if (bal?.PesoActual.HasValue == true)
                        {
                            PesoBruto = (float)bal.PesoActual.Value;
                            PesoNeto = PesoBruto - PesoTara;
                            
                            if (ImagenesCapturadas != null && ImagenesCapturadas.Any())
                            {
                                foreach (var stream in ImagenesCapturadas)
                                {
                                    stream?.Dispose();
                                }
                                ImagenesCapturadas.Clear();
                            }

                            FotoFrontal = null;
                            FotoCarga = null;

                            ImagenesCapturadas = await _imageAuditService.CapturarImagenesAsync(bal.Nombre);

                            if (ImagenesCapturadas != null && ImagenesCapturadas.Count > 0)
                            {
                                FotoFrontal = ConvertirStreamAImagen(ImagenesCapturadas[0]);
                                if (ImagenesCapturadas.Count > 1)
                                {
                                    FotoCarga = ConvertirStreamAImagen(ImagenesCapturadas[1]);
                                }
                            }
                        }
                        else
                        {
                            await _dialogService.ShowWarning("No se encontraron pesos disponibles");
                        }
                    })
                };

                BalanzasInfo.Add(balanzaInfo);
                OnPropertyChanged(nameof(PrimeraBalanza));

                // PESO DE PRUEBA
                balanzaInfo.PesoActual = 100.0m;
                balanzaInfo.Conectada = true;
                balanzaInfo.EsEstable = true;

                _serialPortService.OnPesosLeidos += OnPesoLeido;
                _serialPortService.OnEstabilidadCambiada += OnEstabilidadCambiada;

                var ultimasLecturas = _serialPortService.ObtenerUltimasLecturas();
                if (ultimasLecturas.Any())
                {
                    OnPesoLeido(ultimasLecturas);
                }
                var estabilidadActual = _serialPortService.ObtenerEstabilidadActual();
                if (estabilidadActual.Any())
                {
                    OnEstabilidadCambiada(estabilidadActual);
                }

                _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar lectura de balanza: {ex.Message}");
        }
    }

    private void OnEstabilidadCambiada(Dictionary<string, bool> estabilidades)
    {
        Application.Current.Dispatcher.Invoke(() =>
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

    private void OnPesoLeido(Dictionary<string, string> lecturas)
    {
        Application.Current.Dispatcher.Invoke(() =>
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

    public List<System.IO.MemoryStream> ImagenesCapturadas { get; private set; } = new();

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            if (!MaterialSeleccionado.HasValue)
            {
                _dialogService.ShowWarning("Debe seleccionar un material");
                return;
            }

            if (!UnidadMedidaSeleccionada.HasValue)
            {
                _dialogService.ShowWarning("Debe seleccionar una unidad de medida");
                return;
            }

            if (PesoBruto <= 0)
            {
                _dialogService.ShowWarning("Debe capturar el peso");
                return;
            }

            var confirmar = await _dialogService.ShowConfirm(
                "¿Confirmar registro de pesada?",
                "Confirmar Registro");

            if (!confirmar)
                return;

            LoadingService?.StartLoading();
            var produccion = new Pde
            {
                action = ActionType.Create,
                pde_bie_id = MaterialSeleccionado.Value,
                pde_pb = PesoBruto,
                pde_pt = PesoTara,
                pde_pn = PesoNeto,
                pde_t6m_id = UnidadMedidaSeleccionada,
                pes_veh_id = Pes_veh_id,
                pes_fecha = DateTime.Now,
                pde_obs = Observaciones,
                files = _imageAuditService.ConvertirAFormFiles(ImagenesCapturadas)
            };

            var response = await _produccionService.SaveProduccionAsync(produccion);

            if (response.Data != null)
            {
                if (ImagenesCapturadas.Any())
                {
                    await _imageAuditService.GuardarImagenesLocalmenteAsync(
                        ImagenesCapturadas,
                        response.Data.pde_path,
                        response.Data.pde_media);
                }

                _dialogService.ShowSuccess("Registro guardado exitosamente");
                await MostrarPdfAsync(response.Data.pde_pes_id);
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this)?.Close();
            }
            else
            {
                _dialogService.ShowError(response.Meta.msg ?? "Error al guardar el registro");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error al guardar: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this)?.Close();
    }

    private async Task MostrarPdfAsync(int pesajeId)
    {
        try
        {
            _loadingService.StartLoading();
            var pdfData = await _produccionSearchService.GenerateReportPdfAsync(pesajeId);

            if (pdfData == null || pdfData.Length == 0)
            {
                _dialogService.ShowWarning("No se pudo generar el PDF");
                return;
            }

            _loadingService.StopLoading();
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(
                pdfData,
                $"Producción - Registro Rápido");

            pdfViewer.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Delete)
                {
                    pdfViewer.Close();
                    e.Handled = true;
                }
            };

            pdfViewer.ShowDialog();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error al generar PDF: {ex.Message}");
        }
        finally
        {
            _loadingService.StopLoading();
        }
    }

    private System.Windows.Media.ImageSource? ConvertirStreamAImagen(System.IO.MemoryStream stream)
    {
        try
        {
            if (stream == null || stream.Length == 0) return null;
            
            var image = new System.Windows.Media.Imaging.BitmapImage();
            image.BeginInit();
            image.StreamSource = new System.IO.MemoryStream(stream.ToArray());
            image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch { return null; }
    }

    public void Cleanup()
    {
        try
        {
            _serialPortService.DetenerLectura();
            _serialPortService.OnPesosLeidos -= OnPesoLeido;
            _serialPortService.OnEstabilidadCambiada -= OnEstabilidadCambiada;
        }
        catch { }
    }
}
