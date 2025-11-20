using CacelApp.Services.Dialog;
using CacelApp.Services.Image;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.ImageViewer;
using CacelApp.Shared.Controls.PdfViewer;
using CacelApp.Shared.Controls.DataTable;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Shared.Entities;
using Infrastructure.Services.Balanza;
using Infrastructure.Services.Shared;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CacelApp.Views.Modulos.Balanza.Entities;

namespace CacelApp.Views.Modulos.Balanza;

/// <summary>
/// ViewModel para el módulo de Balanza
/// Implementa MVVM pattern con MVVM Community Toolkit
/// Separación clara entre lógica de presentación y lógica de negocio
/// </summary>
public partial class BalanzaModel : ViewModelBase
{
    private readonly IBalanzaReadService _balanzaReadService;
    private readonly IBalanzaWriteService _balanzaWriteService;
    private readonly IBalanzaReportService _balanzaReportService;
    private readonly ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    
    // Diccionario para guardar los registros completos con sus relaciones
    private readonly Dictionary<int, Core.Repositories.Balanza.Entities.Baz> _registrosCompletos = new();

    // Propiedades Observable para Filtros
    [ObservableProperty]
    private DateTime? fechaInicio = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    private DateTime? fechaFinal = DateTime.Now;

    [ObservableProperty]
    private string? filtroPlaca;

    [ObservableProperty]
    private string? filtroCliente;

  

    #region DataTable Reutilizable

    /// <summary>
    /// ViewModel de la tabla reutilizable
    /// </summary>
    public DataTableViewModel<BalanzaItemDto> TableViewModel { get; } = new();

    /// <summary>
    /// Configuración de columnas para la tabla
    /// </summary>
    public ObservableCollection<DataTableColumn> TableColumns { get; }

    /// <summary>
    /// Acceso al registro seleccionado desde la tabla
    /// Usa SelectedItemData que se actualiza automáticamente
    /// </summary>
    public BalanzaItemDto? RegistroSeleccionado => TableViewModel.SelectedItemData;

    #endregion

    // Propiedades de Estadísticas

    [ObservableProperty]
    private int totalRegistros;

    [ObservableProperty]
    private decimal montoTotal;

    [ObservableProperty]
    private decimal pesoNetoPromedio;

    // Propiedades de Edición

    [ObservableProperty]
    private bool esEdicion;

    [ObservableProperty]
    private BalanzaRegistroDto? registroEditando;

    // Comandos
    public IAsyncRelayCommand BuscarCommand { get; }
    public IAsyncRelayCommand AgregarCommand { get; }
    public IAsyncRelayCommand<BalanzaItemDto> EditarCommand { get; }
    public IAsyncRelayCommand<BalanzaItemDto> VerImagenesCommand { get; }
    public IAsyncRelayCommand<BalanzaItemDto> PrevisualizarPdfCommand { get; }
    public IAsyncRelayCommand CancelarCommand { get; }
    public IAsyncRelayCommand GuardarCommand { get; }

    public BalanzaModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IBalanzaReadService balanzaReadService,
        IBalanzaWriteService balanzaWriteService,
        IBalanzaReportService balanzaReportService,
        ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService) : base(dialogService, loadingService)
    {
        _balanzaReadService = balanzaReadService ?? throw new ArgumentNullException(nameof(balanzaReadService));
        _balanzaWriteService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));

        // Inicializar comandos primero (antes de configurar las columnas)
        BuscarCommand = new AsyncRelayCommand(BuscarRegistrosAsync);
        AgregarCommand = new AsyncRelayCommand(AgregarRegistroAsync);
        EditarCommand = new AsyncRelayCommand<BalanzaItemDto>(EditarRegistroAsync);       
        VerImagenesCommand = new AsyncRelayCommand<BalanzaItemDto>(VerImagenesAsync);
        PrevisualizarPdfCommand = new AsyncRelayCommand<BalanzaItemDto>(PrevisualizarPdfAsync);
        CancelarCommand = new AsyncRelayCommand(CancelarAsync);
        GuardarCommand = new AsyncRelayCommand(GuardarRegistroAsync);

        // Configurar columnas de la tabla
        TableColumns = new ObservableCollection<DataTableColumn>
        {
            new DataTableColumn
            {
                PropertyName = "Baz.baz_des",
                Header = "CÓDIGO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Hyperlink,
                HyperlinkCommand = PrevisualizarPdfCommand,
                HyperlinkToolTip = "Click para previsualizar el reporte PDF",
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_veh_id",
                Header = "PLACA",
                Width = "0.6*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_ref",
                Header = "REFERENCIA",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_fecha",
                Header = "FECHA",
                Width = "1*",
                ColumnType = DataTableColumnType.Date,
                StringFormat = "dd/MM/yyyy HH:mm",
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_pb",
                Header = "P. BRUTO",
                Width = "0.7*",
                ColumnType = DataTableColumnType.Number,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                ShowTotal = true,
                DisplayPriority = 3
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_pt",
                Header = "P. TARA",
                Width = "0.7*",
                ColumnType = DataTableColumnType.Number,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                ShowTotal = true,
                DisplayPriority = 3
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_pn",
                Header = "P. NETO",
                Width = "0.7*",
                ColumnType = DataTableColumnType.Number,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                ShowTotal = true,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_tipo",
                Header = "OPERACIÓN",
                Width = "1.2*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_monto",
                Header = "MONTO",
                Width = "0.6*",
                ColumnType = DataTableColumnType.Number,
                HorizontalAlignment = "Right",
                ShowTotal = true,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_gus_des",
                Header = "USUARIO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 3
            },
            new DataTableColumn
            {
                PropertyName = "Baz.baz_status",
                Header = "ESTADO",
                Width = "0.5*",
                ColumnType = DataTableColumnType.BooleanStatus,
                HorizontalAlignment = "Center",
                DisplayPriority = 1,

            },
            new DataTableColumn
            {
                PropertyName = "Acciones",
                Header = "ACCIONES",
                Width = "0.7*",
                ColumnType = DataTableColumnType.Actions,
                HorizontalAlignment = "Center",
                CanSort = false,
                DisplayPriority = 1,
                ShowInExpandedView = false,
                ActionButtons = new List<DataTableActionButton>
                {
                    new DataTableActionButton
                    {
                        Icon = MaterialDesignThemes.Wpf.PackIconKind.Pencil,
                        Tooltip = "Editar",
                        Command = EditarCommand,
                        IconSize= 24
                        
                    },
                    new DataTableActionButton
                    {
                        Icon = MaterialDesignThemes.Wpf.PackIconKind.Eye,
                        Tooltip = "Ver imágenes",
                        Command = VerImagenesCommand,
                        Foreground = System.Windows.Application.Current.TryFindResource("PrimaryHueMidBrush") as System.Windows.Media.Brush,
                        IconSize= 24
                    }
                }
            }
        };

        // Nota: RegistroSeleccionado se actualiza automáticamente via SelectedItemData
        // del DataTableViewModel (usa NotifyPropertyChangedFor)

        // Cargar datos iniciales
        _ = BuscarRegistrosAsync();
    }

    /// <summary>
    /// Busca registros de balanza con los filtros especificados
    /// </summary>
    private async Task BuscarRegistrosAsync()
    {
        try
        {
            var registros = await _balanzaReadService.ObtenerRegistrosAsync(
                FechaInicio,
                FechaFinal,
                FiltroPlaca,
                FiltroCliente,
                null);

            // Limpiar y guardar los registros completos
            _registrosCompletos.Clear();
            foreach (var reg in registros)
            {
                _registrosCompletos[reg.baz_id] = reg;
            }

            // Mapear a DTOs para presentación (ahora solo wrapeamos la entidad completa)
            var items = registros.Select((reg, index) => new BalanzaItemDto
            {
                Index = index + 1,
                Baz = reg  // ⬅️ Asignamos la entidad completa
            }).ToList();

            // Cargar datos en la tabla reutilizable
            TableViewModel.SetData(items);

            // Actualizar estadísticas
            ActualizarEstadisticas(items);
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al buscar registros");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Abre el diálogo para agregar un nuevo registro
    /// </summary>
    private async Task AgregarRegistroAsync()
    {
        try
        {
            // Crear el ViewModel para la ventana de mantenimiento
            var mantViewModel = new MantBalanzaModel(
                DialogService,
                LoadingService,
                _balanzaWriteService,
                _balanzaReportService,
                _selectOptionService,
                _imageLoaderService);

            // Crear y mostrar la ventana
            var mantWindow = new MantBalanza(mantViewModel);
            
            var resultado = mantWindow.ShowDialog();

            // Si se guardó correctamente, recargar la lista
            if (resultado == true)
            {
                await BuscarRegistrosAsync();
                await DialogService.ShowSuccess("Éxito", "Registro creado correctamente");
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError("Error", ex.Message);
        }
    }

    /// <summary>
    /// Abre el diálogo para editar el registro seleccionado
    /// </summary>
    private async Task EditarRegistroAsync(BalanzaItemDto? registro)
    {
        if (registro == null)
        {
            await DialogService.ShowWarning("Selección requerida", "Por favor seleccione un registro para editar");
            return;
        }

        try
        {
            // Ahora el registro ya contiene la entidad completa Baz
            var registroCompleto = registro.Baz;
            
            // Crear el ViewModel para la ventana de mantenimiento
            var mantViewModel = new MantBalanzaModel(
                DialogService,
                LoadingService,
                _balanzaWriteService,
                _balanzaReportService,
                _selectOptionService,
                _imageLoaderService);

            // Cargar los datos del registro completo con todas las relaciones (veh, age, tra, etc.)
            mantViewModel.CargarRegistroCompleto(registroCompleto);

            // Crear y mostrar la ventana
            var mantWindow = new MantBalanza(mantViewModel);
            
            var resultado = mantWindow.ShowDialog();

            // Si se actualizó correctamente, recargar la lista
            if (resultado == true)
            {
                await BuscarRegistrosAsync();
                await DialogService.ShowSuccess("Éxito", "Registro actualizado correctamente");
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError("Error", ex.Message);
        }
    }


    /// <summary>
    /// Guarda el registro que está siendo editado
    /// </summary>
    private async Task GuardarRegistroAsync()
    {
        if (RegistroEditando == null)
            return;

        try
        {
            LoadingService.StartLoading();

            // Validar datos básicos
            if (string.IsNullOrWhiteSpace(RegistroEditando.VehiculoId))
            {
                throw new InvalidOperationException("El vehículo es requerido");
            }

            // Aquí iría la lógica de guardado
            // await _balanzaWriteService.CrearRegistroAsync(...);

            await DialogService.ShowSuccess("Éxito", "Registro guardado correctamente");
            EsEdicion = false;
            RegistroEditando = null;
            await BuscarRegistrosAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError("Error", ex.Message);
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Cancela la edición actual
    /// </summary>
    private async Task CancelarAsync()
    {
        EsEdicion = false;
        RegistroEditando = null;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Previsualiza el reporte PDF del registro seleccionado
    /// </summary>
    private async Task PrevisualizarPdfAsync(BalanzaItemDto? registro)
    {
        if (registro == null)
        {
            await DialogService.ShowWarning("Selección requerida", "Por favor seleccione un registro");
            return;
        }

        try
        {
            LoadingService.StartLoading();

            var pdfBytes = await _balanzaReportService.GenerarReportePdfAsync(registro.Baz.baz_id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("Sin datos", "No se pudo generar el reporte PDF");
                return;
            }

            // Crear y abrir ventana de previsualización PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfBytes, $"Reporte {registro.Baz.baz_des}");
            pdfViewer.Show();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError("Error", $"No se pudo previsualizar el PDF: {ex.Message}");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Muestra las imágenes capturadas del registro de balanza
    /// </summary>
    private async Task VerImagenesAsync(BalanzaItemDto? registro)
    {
        if (registro == null)
        {
            await DialogService.ShowWarning( "Por favor seleccione un registro", "Selección requerida");
            return;
        }

        try
        {
            LoadingService.StartLoading();

            // baz_media contiene las imágenes de pesaje
            var bazMedia = registro.Baz.baz_media ?? string.Empty;
            var bazMedia1 = registro.Baz.baz_media1 ?? string.Empty;

            // Si ambos están vacíos, no hay imágenes
            if (string.IsNullOrEmpty(bazMedia) && string.IsNullOrEmpty(bazMedia1))
            {
                LoadingService.StopLoading();
                await DialogService.ShowInfo("El registro no tiene capturas de cámara registradas", "Sin imágenes");
                return;
            }

            // Cargar imágenes de pesaje (baz_media)
            var imagenesPesaje = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia) && !string.IsNullOrEmpty(registro.Baz.baz_path))
            {
                imagenesPesaje = await _imageLoaderService.CargarImagenesAsync(
                    registro.Baz.baz_path, 
                    bazMedia);
            }

            // Cargar imágenes de destare (baz_media1)
            var imagenesDestare = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia1) && !string.IsNullOrEmpty(registro.Baz.baz_path))
            {
                imagenesDestare = await _imageLoaderService.CargarImagenesAsync(
                    registro.Baz.baz_path, 
                    bazMedia1);
            }

            LoadingService.StopLoading();

            // Verificar si se cargaron imágenes
            if (!imagenesPesaje.Any() && !imagenesDestare.Any())
            {
                await DialogService.ShowWarning("No se pudieron cargar las imágenes del registro", "Sin imágenes");
                return;
            }

            // Crear ViewModel y mostrar ventana
            var viewModel = new ImageViewerViewModel(
                imagenesPesaje, 
                imagenesDestare.Any() ? imagenesDestare : null,
                $"Registro: {registro.Baz.baz_des} - Placa: {registro.Baz.baz_veh_id}");

            var imageViewer = new ImageViewerWindow(viewModel);
            imageViewer.ShowDialog();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"No se pudieron cargar las imágenes: {ex.Message}", "Error");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Genera un reporte de los registros
    /// </summary>


    /// <summary>
    /// Actualiza las estadísticas mostradas
    /// </summary>
    private void ActualizarEstadisticas(List<BalanzaItemDto> registros)
    {
        TotalRegistros = registros.Count;
        MontoTotal = registros.Sum(r => r.Baz.baz_monto);
        PesoNetoPromedio = registros.Count > 0 ? registros.Average(r => r.Baz.baz_pn ?? 0) : 0;
    }
}
