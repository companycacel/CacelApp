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
    private readonly IConfigurationService _configurationService;
    private readonly Infrastructure.Services.Shared.ISelectOptionService _selectOptionService;

    #region Propiedades Observables

    [ObservableProperty]
    private ObservableCollection<SelectOption> _materiales = new();

    [ObservableProperty]
    private ObservableCollection<SelectOption> _tiposEmpaque = new();

    [ObservableProperty]
    private int? _materialSeleccionado;

    [ObservableProperty]
    private string? _materialCodigo;

    [ObservableProperty]
    private string? _materialDescripcion;

    [ObservableProperty]
    private string? _tipoEmpaqueSeleccionado;

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
        IConfigurationService configurationService,
        Infrastructure.Services.Shared.ISelectOptionService selectOptionService) 
        : base(dialogService, loadingService)
    {
        _dialogService = dialogService;
        _loadingService = loadingService;
        _produccionService = produccionService;
        _produccionSearchService = produccionSearchService;
        _serialPortService = serialPortService;
        _configurationService = configurationService;
        _selectOptionService = selectOptionService;

        InicializarDatos();
        IniciarLecturaBalanza();
    }

    private void InicializarDatos()
    {
        // Cargar tipos de empaque
        TiposEmpaque = new ObservableCollection<SelectOption>
        {
            new SelectOption { Value = "PACA", Label = "PACA" },
            new SelectOption { Value = "SACA", Label = "SACA" },
            new SelectOption { Value = "PALETA", Label = "PALETA" }
        };

        // Cargar materiales mockup (TODO: reemplazar con datos reales)
        // Estructura: Value (int) = ID, Label (string) = Descripción, Ext (object) = Datos adicionales
        Materiales = new ObservableCollection<SelectOption>
        {
            new SelectOption 
            { 
                Value = 1, 
                Label = "PL - Plata", 
                Ext = new { Codigo = "PL", Color = "#DBEAFE", Categoria = "Metal Precioso", Densidad = 10.49 }
            },
            new SelectOption 
            { 
                Value = 2, 
                Label = "CU - Cobre", 
                Ext = new { Codigo = "CU", Color = "#D1FAE5", Categoria = "Metal Base", Densidad = 8.96 }
            },
            new SelectOption 
            { 
                Value = 3, 
                Label = "AL - Aluminio", 
                Ext = new { Codigo = "AL", Color = "#FECDD3", Categoria = "Metal Ligero", Densidad = 2.70 }
            },
            new SelectOption 
            { 
                Value = 4, 
                Label = "ZN - Zinc", 
                Ext = new { Codigo = "ZN", Color = "#DBEAFE", Categoria = "Metal Base", Densidad = 7.14 }
            },
            new SelectOption 
            { 
                Value = 5, 
                Label = "BR - Bronce", 
                Ext = new { Codigo = "BR", Color = "#FED7AA", Categoria = "Aleación", Densidad = 8.73 }
            },
            new SelectOption 
            { 
                Value = 6, 
                Label = "NI - Níquel", 
                Ext = new { Codigo = "NI", Color = "#FECDD3", Categoria = "Metal Base", Densidad = 8.91 }
            }
        };
    }

    private async void IniciarLecturaBalanza()
    {
        try
        {
            // Obtener configuración de balanza principal
            var sede = await _configurationService.GetSedeActivaAsync();
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
            var sede = await _configurationService.GetSedeActivaAsync();
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
                MaterialCodigo = extData.Codigo;
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
    private void CapturarPeso()
    {
        try
        {
            if (PesoActual <= 0)
            {
                _dialogService.ShowWarning("El peso actual debe ser mayor a 0");
                return;
            }

            PesoBruto = PesoActual;
            ActualizarPesos();
            
            _dialogService.ShowSuccess($"Peso capturado: {PesoActual:F2} KG");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Error al capturar peso: {ex.Message}");
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

            if (string.IsNullOrEmpty(TipoEmpaqueSeleccionado))
            {
                _dialogService.ShowWarning("Debe seleccionar un tipo de empaque");
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
                pde_bie_cod = MaterialCodigo ?? "",
                pde_bie_des = MaterialDescripcion,
                pde_pb = PesoBruto,
                pde_pt = PesoTara,
                pde_pn = PesoNeto,
                pde_obs = $"Tipo Empaque: {TipoEmpaqueSeleccionado}",
                pes_fecha = DateTime.Now,
                pde_tipo = 1 // Tipo producción
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

    /// <summary>
    /// Método auxiliar para obtener datos adicionales del material seleccionado
    /// Ejemplo de cómo acceder a las propiedades del objeto Ext
    /// </summary>
    private dynamic? ObtenerDatosAdicionales(int materialId)
    {
        var material = Materiales.FirstOrDefault(m => m.Value?.ToString() == materialId.ToString());
        if (material?.Ext != null)
        {
            // El objeto Ext contiene: Codigo, Color, Categoria, Densidad
            // Ejemplo de uso:
            // dynamic datos = ObtenerDatosAdicionales(materialId);
            // string codigo = datos.Codigo;
            // string color = datos.Color;
            // string categoria = datos.Categoria;
            // double densidad = datos.Densidad;
            return material.Ext;
        }
        return null;
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
