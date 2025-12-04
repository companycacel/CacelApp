using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Infrastructure.Services.Produccion;
using Infrastructure.Services.Shared;
using System.Collections.ObjectModel;
using System.Windows;
using Application = System.Windows.Application;

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
    private readonly ICameraService _cameraService;
    private readonly Infrastructure.Services.Shared.ISelectOptionService _selectOptionService;

    #region Propiedades Observables

    [ObservableProperty]
    private ObservableCollection<SelectOption> _materiales = new();

    [ObservableProperty]
    private ObservableCollection<SelectOption> _materialesPaginados = new();

    [ObservableProperty]
    private int _paginaActual = 1;

    [ObservableProperty]
    private int _totalPaginas = 1;

    private const int MATERIALES_POR_PAGINA = 6;

    [ObservableProperty]
    private ObservableCollection<SelectOption> _unidadesMedida = new();

    // Propiedad computada para FormRadioGroup
    public ObservableCollection<CacelApp.Shared.Controls.Form.RadioOption> UnidadesMedidaRadio
    {
        get
        {
            var radioOptions = new ObservableCollection<CacelApp.Shared.Controls.Form.RadioOption>();
            foreach (var item in UnidadesMedida)
            {
                radioOptions.Add(new CacelApp.Shared.Controls.Form.RadioOption
                {
                    Label = item.Label ?? "",
                    Value = item.Value
                });
            }
            return radioOptions;
        }
    }

    [ObservableProperty]
    private int? _materialSeleccionado;

    [ObservableProperty]
    private string? _materialCodigo;

    [ObservableProperty]
    private string? _materialDescripcion;

    [ObservableProperty]
    private object? _materialExtData;

    [ObservableProperty]
    private int? _unidadMedidaSeleccionada;

    [ObservableProperty]
    private float _pesoActual;

    [ObservableProperty]
    private float _pesoBruto;

    [ObservableProperty]
    private float _pesoTara;

    [ObservableProperty]
    private float _pesoNeto;

    [ObservableProperty]
    private ObservableCollection<PesoCapturado> _pesosCapturados = new();

    [ObservableProperty]
    private bool _isBusy;

    #endregion

    public RegistroRapidoProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IProduccionService produccionService,
        IProduccionSearchService produccionSearchService,
        ISerialPortService serialPortService,
        IConfigurationService configService,
        Infrastructure.Services.Shared.ISelectOptionService selectOptionService,
        ICameraService cameraService) 
        : base(dialogService, loadingService)
    {
        _dialogService = dialogService;
        _loadingService = loadingService;
        _produccionService = produccionService;
        _produccionSearchService = produccionSearchService;
        _serialPortService = serialPortService;
        _configService = configService;
        _selectOptionService = selectOptionService;
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
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
                
                // Extraer código del ExtData
                if (material.Ext != null)
                {
                    dynamic extData = material.Ext;
                    //MaterialCodigo = extData.bie_id;
                }
            }
        }
    }

    partial void OnPesoTaraChanged(float value)
    {
        // Actualizar peso neto automáticamente
        PesoNeto = PesoBruto - PesoTara;
    }

    partial void OnUnidadesMedidaChanged(ObservableCollection<SelectOption> value)
    {
        // Notificar que UnidadesMedidaRadio también cambió
        OnPropertyChanged(nameof(UnidadesMedidaRadio));
    }

    partial void OnMaterialesChanged(ObservableCollection<SelectOption> value)
    {
        ActualizarPaginacion();
    }

    private async Task InicializarDatosAsync()
    {
        try
        {
            IsBusy = true;
            var umeds = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Umedida);
            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UnidadesMedida.Clear();
                foreach (var u in umeds)
                {
                    var valorInt = u.Value is int intVal ? intVal : int.Parse(u.Value?.ToString() ?? "0");
                    UnidadesMedida.Add(new SelectOption { Value = valorInt, Label = u.Label });
                }
            });
        }
        catch (Exception ex)
        {
           await _dialogService.ShowError($"Error al cargar unidades de medida: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }

        var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material,null,new { bie_tipo =3});
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
        
        ActualizarPaginacion();
    }

    private async void IniciarLecturaBalanza()
    {
        try
        {
            // Obtener configuración de balanza principal
            var sede = await _configService.GetSedeActivaAsync();
            if (sede != null && sede.Balanzas.Any())
            {
                _serialPortService.OnPesosLeidos += OnPesoLeido;
                _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar lectura de balanza: {ex.Message}");
        }
    }

    private void OnPesoLeido(Dictionary<string, string> lecturas)
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
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
                        // Usar la primera balanza configurada
                        if (sede.Balanzas.Count > 0 && balanza.Id == sede.Balanzas[0].Id)
                        {
                            PesoActual = peso;
                        }
                    }
                }
            }
        });
    }

    [RelayCommand]
    private void SeleccionarMaterial(object parameter)
    {
        // Convertir el parámetro a int (puede venir como string desde XAML)
        int materialId;
        if (parameter is int id)
        {
            materialId = id;
        }
        else if (parameter is string strId && int.TryParse(strId, out int parsedId))
        {
            materialId = parsedId;
        }
        else
        {
            return; // Parámetro inválido
        }

        MaterialSeleccionado = materialId;
        var material = Materiales.FirstOrDefault(m => m.Value?.ToString() == materialId.ToString());
        if (material != null)
        {
            // Extraer el código del objeto Ext usando dynamic
            if (material.Ext != null)
            {
                dynamic extData = material.Ext;
                //MaterialCodigo = extData.Codigo;
            }
            MaterialDescripcion = material.Label;
        }
    }

    [RelayCommand]
    private void ActualizarPesos()
    {
        PesoNeto = PesoBruto - PesoTara;
    }

    [RelayCommand]
    private async Task CapturarPesoAsync()
    {
        try
        {
            if (PesoActual <= 0)
            {
                await _dialogService.ShowWarning("El peso actual debe ser mayor a 0");
                return;
            }

            PesoBruto = PesoActual;
            PesoNeto = PesoBruto - PesoTara; 

            await CapturarFotosCamarasAsync();
            await _dialogService.ShowSuccess($"Peso capturado: {PesoActual:F2} KG");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError($"Error al capturar peso: {ex.Message}");
        }
    }

    public List<System.IO.MemoryStream> ImagenesCapturadas { get; private set; } = new();
    private async Task CapturarFotosCamarasAsync()
    {
        try
        {

            // Limpiar memoria de imágenes anteriores antes de capturar nuevas
            if (ImagenesCapturadas != null && ImagenesCapturadas.Any())
            {
                foreach (var stream in ImagenesCapturadas)
                {
                    stream?.Dispose();
                }
                ImagenesCapturadas.Clear();
            }

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

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            // Validaciones
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

            // Mostrar diálogo de confirmación
            var confirmar = await _dialogService.ShowConfirm(
                "Confirmar Registro",
                "¿Confirmar registro de pesada?");

            if (!confirmar)
                return;

            IsBusy = true;

            // Crear entidad de producción
            var produccion = new Pde
            {
                action = ActionType.Create,
                pde_bie_id = MaterialSeleccionado.Value,
                pde_pb = PesoBruto,
                pde_pt = PesoTara,
                pde_pn = PesoNeto,
                pde_t6m_id = UnidadMedidaSeleccionada,
                pes_fecha = DateTime.Now,
                files = ImagenesCapturadas.Select((ms, index) =>
                {
                    var bytes = ms.ToArray();
                    return (Microsoft.AspNetCore.Http.IFormFile)new SimpleFormFile(bytes, "files", $"{index + 1}.jpg");
                }).ToList()
            };

            // Guardar
            var response = await _produccionService.SaveProduccionAsync(produccion);

            if (response.Data != null)
            {
                _dialogService.ShowSuccess("Registro guardado exitosamente");

                // Generar y mostrar PDF
                await MostrarPdfAsync(response.Data.pde_pes_id);

                // Cerrar ventana
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
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this)?.Close();
    }

    /// <summary>
    /// Genera y muestra el PDF del registro de producción
    /// </summary>
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

            // Abrir visor de PDF con soporte para tecla Supr
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(
                pdfData, 
                $"Producción - Registro Rápido");
            
            // Agregar manejo de tecla Supr para cerrar y regresar
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

    public void Cleanup()
    {
        _serialPortService.OnPesosLeidos -= OnPesoLeido;
    }

    private void ActualizarPaginacion()
    {
        if (Materiales.Count == 0)
        {
            TotalPaginas = 1;
            PaginaActual = 1;
            MaterialesPaginados.Clear();
            return;
        }

        TotalPaginas = (int)Math.Ceiling((double)Materiales.Count / MATERIALES_POR_PAGINA);
        
        // Asegurar que la página actual esté en rango
        if (PaginaActual > TotalPaginas)
            PaginaActual = TotalPaginas;
        if (PaginaActual < 1)
            PaginaActual = 1;

        CargarMaterialesPagina();
    }

    private void CargarMaterialesPagina()
    {
        var skip = (PaginaActual - 1) * MATERIALES_POR_PAGINA;
        var materialesPagina = Materiales.Skip(skip).Take(MATERIALES_POR_PAGINA).ToList();

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MaterialesPaginados.Clear();
            foreach (var material in materialesPagina)
            {
                MaterialesPaginados.Add(material);
            }
        });
    }

    [RelayCommand]
    private void PaginaAnterior()
    {
        if (PaginaActual > 1)
        {
            PaginaActual--;
            CargarMaterialesPagina();
        }
    }

    [RelayCommand]
    private void PaginaSiguiente()
    {
        if (PaginaActual < TotalPaginas)
        {
            PaginaActual++;
            CargarMaterialesPagina();
        }
    }
}



/// <summary>
/// Clase para representar un peso capturado
/// </summary>
public class PesoCapturado
{
    public float PesoBruto { get; set; }
    public float PesoTara { get; set; }
    public float PesoNeto { get; set; }
    public DateTime FechaHora { get; set; }
}
