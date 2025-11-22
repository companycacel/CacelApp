using CacelApp.Services.Dialog;
using CacelApp.Services.Image;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.DataTable;
using CacelApp.Shared.Controls.ImageViewer;
using CacelApp.Shared.Controls.PdfViewer;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Balanza.Entities;
using Core.Shared.Entities;
using Infrastructure.Services.Balanza;
using Infrastructure.Services.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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
    private DateTime? fechaInicio = DateTime.Now.AddMonths(-3);

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


    // Comandos
    public IAsyncRelayCommand BuscarCommand { get; }
    public IAsyncRelayCommand AgregarCommand { get; }
    public IAsyncRelayCommand<BalanzaItemDto> EditarCommand { get; }
    public IAsyncRelayCommand<BalanzaItemDto> VerImagenesCommand { get; }
    public IAsyncRelayCommand<BalanzaItemDto> PrevisualizarPdfCommand { get; }


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
    

        TableColumns = new ObservableCollection<DataTableColumn>
        {
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_des, Header="CÓDIGO", Width="0.8*", Type=DataTableColumnType.Hyperlink, Command=PrevisualizarPdfCommand, Tooltip="Click para previsualizar el reporte PDF", Priority=1 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_veh_id, Header="PLACA", Width="0.6*", Priority=1 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_ref, Header="REFERENCIA", Width="0.8*", Priority=2 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_fecha, Header="FECHA", Width="1*", Type=DataTableColumnType.Date, Format="dd/MM/yyyy HH:mm", Priority=2 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_pb, Header="P. BRUTO", Width="0.7*", Type=DataTableColumnType.Number, Format="N2", Align="Right", ShowTotal=true, Priority=3 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_pt, Header="P. TARA", Width="0.7*", Type=DataTableColumnType.Number, Format="N2", Align="Right", ShowTotal=true, Priority=3 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_pn, Header="P. NETO", Width="0.7*", Type=DataTableColumnType.Number, Format="N2", Align="Right", ShowTotal=true, Priority=2 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_tipo_des, Header="OPERACIÓN", Width="1.2*", Priority=2 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_monto, Header="MONTO", Width="0.6*", Type=DataTableColumnType.Number, Align="Right", ShowTotal=true, Priority=2 },
            new ColDef<BalanzaItemDto>{ Key=x=>x.baz_gus_des, Header="USUARIO", Width="0.8*", Priority=3 },
            new ColDef<BalanzaItemDto>{
                Key = x => x.baz_status,
                Header = "ESTADO",
                Width = "0.5*",
                Type = DataTableColumnType.BooleanStatus,
                Align = "Center",
                Priority = 1,
                Status = new StatusIndicator {
                    BooleanTrueIcon = PackIconKind.CheckCircleOutline,
                    BooleanFalseIcon = PackIconKind.CloseCircleOutline,
                    BooleanTrueColor = "#4CAF50",
                    BooleanFalseColor = "#F44336",
                    BooleanTrueText = "Completado",
                    BooleanFalseText = "Pendiente"
                }
            },
            new ColDef<BalanzaItemDto>
            {
                Key=x=>x.Index, Header="ACCIONES", Width="0.7*", Priority=1,
                Actions = new List<ActionDef>
                {
                    new ActionDef{ Icon=PackIconKind.Pencil, Tooltip="Editar", Command=EditarCommand, IconSize=24 },
                    new ActionDef{ Icon=PackIconKind.Eye, Tooltip="Ver imágenes", Command=VerImagenesCommand, IconSize=24 }
                }
            }
        };

        _ = BuscarRegistrosAsync();
    }

    /// <summary>
    /// Busca registros de balanza con los filtros especificados
    /// </summary>
    private async Task BuscarRegistrosAsync()
    {

        // 1. Función de Obtención de Datos (fetcher)
        Func<Task<IEnumerable<Baz>>> dataFetcher =
            () => _balanzaReadService.ObtenerRegistrosAsync(
                FechaInicio, FechaFinal, FiltroPlaca, FiltroCliente, null);

        // 2. Función de Mapeo de DTOs (mapper) - RÁPIDO Y PRAGMÁTICO
        Func<Baz, BalanzaItemDto> dtoMapper = (reg) =>
        {
            var dto = new BalanzaItemDto();
            ObjectMapper.CopyProperties(reg, dto);
            return dto;
        };

        // 3. Función para extraer el ID
        Func<Baz, int> idExtractor = (reg) => reg.baz_id;

        // Ejecutar la carga centralizada
        await ExecuteDataLoadAsync(
            dataFetcher,
            dtoMapper,
            idExtractor,
            _registrosCompletos,
            TableViewModel,
            ActualizarEstadisticas,
            "Error al buscar registros");
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
                _balanzaReadService,
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
    private async Task EditarRegistroAsync(BalanzaItemDto? item)
    {
        if (item == null)
        {
            await DialogService.ShowWarning("Selección requerida", "Por favor seleccione un registro para editar");
            return;
        }

        try
        {
            // Obtener el registro completo desde el diccionario
            if (!_registrosCompletos.TryGetValue(item.baz_id, out var registroCompleto))
            {
                await DialogService.ShowError("Error", "No se pudo cargar el registro completo");
                return;
            }
     
            // Crear el ViewModel para la ventana de mantenimiento
            var mantViewModel = new MantBalanzaModel(
                DialogService,
                LoadingService,
                _balanzaReadService,
                _balanzaWriteService,
                _balanzaReportService,
                _selectOptionService,
                _imageLoaderService);

            // IMPORTANTE: Cargar datos iniciales ANTES de cargar el registro
            // Esto asegura que las colecciones (Vehiculos, TiposPago) estén pobladas
            await mantViewModel.CargarDatosInicialesAsync();

            // Ahora cargar los datos del registro completo con todas las relaciones
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

            var pdfBytes = await _balanzaReportService.GenerarReportePdfAsync(registro.baz_id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("Sin datos", "No se pudo generar el reporte PDF");
                return;
            }

            // Crear y abrir ventana de previsualización PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfBytes, $"Reporte {registro.baz_des}");
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
            await DialogService.ShowWarning("Por favor seleccione un registro", "Selección requerida");
            return;
        }

        try
        {
            LoadingService.StartLoading();

            // baz_media contiene las imágenes de pesaje
            var bazMedia = registro.baz_media ?? string.Empty;
            var bazMedia1 = registro.baz_media1 ?? string.Empty;

            // Si ambos están vacíos, no hay imágenes
            if (string.IsNullOrEmpty(bazMedia) && string.IsNullOrEmpty(bazMedia1))
            {
                LoadingService.StopLoading();
                await DialogService.ShowInfo("El registro no tiene capturas de cámara registradas", "Sin imágenes");
                return;
            }

            // Cargar imágenes de pesaje (baz_media)
            var imagenesPesaje = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia) && !string.IsNullOrEmpty(registro.baz_path))
            {
                imagenesPesaje = await _imageLoaderService.CargarImagenesAsync(
                    registro.baz_path,
                    bazMedia);
            }

            // Cargar imágenes de destare (baz_media1)
            var imagenesDestare = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia1) && !string.IsNullOrEmpty(registro.baz_path))
            {
                imagenesDestare = await _imageLoaderService.CargarImagenesAsync(
                    registro.baz_path,
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
                $"Registro: {registro.baz_des} - Placa: {registro.baz_veh_id}");

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
        MontoTotal = registros.Sum(r => r.baz_monto);
        PesoNetoPromedio = registros.Count > 0 ? registros.Average(r => r.baz_pn ?? 0) : 0;
    }
}
