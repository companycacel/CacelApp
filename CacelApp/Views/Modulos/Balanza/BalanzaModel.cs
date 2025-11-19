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


    public ObservableCollection<SelectOption> EstadoOptions { get; } = new()
        {
            new SelectOption { Value = null, Label = "Todos" },
            new SelectOption { Value = 1, Label = "Activos" },
            new SelectOption { Value = 0, Label = "Inactivos" }
        };

    [ObservableProperty]
    private SelectOption? selectedEstadoOption;

    // Puedes hacer que FiltroEstado se calcule de esta propiedad
    public int? FiltroEstado => SelectedEstadoOption?.Value;

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
    /// </summary>
    public BalanzaItemDto? RegistroSeleccionado => TableViewModel.SelectedItem?.Item;

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
            // Columna de expansión (siempre visible)
            new DataTableColumn
            {
                PropertyName = "IsExpanded",
                Header = "",
                Width = "80",
                ColumnType = DataTableColumnType.Template,
                TemplateKey = "ExpanderTemplate",
                CanSort = false,
                DisplayPriority = 1,
                ShowInExpandedView = false
            },
            new DataTableColumn
            {
                PropertyName = "Codigo",
                Header = "CÓDIGO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Hyperlink,
                HyperlinkCommand = PrevisualizarPdfCommand,
                HyperlinkToolTip = "Click para previsualizar el reporte PDF",
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Placa",
                Header = "PLACA",
                Width = "0.6*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Referencia",
                Header = "REFERENCIA",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Fecha",
                Header = "FECHA",
                Width = "1*",
                ColumnType = DataTableColumnType.Date,
                StringFormat = "dd/MM/yyyy HH:mm",
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "PesoBruto",
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
                PropertyName = "PesoTara",
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
                PropertyName = "PesoNeto",
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
                PropertyName = "Operacion",
                Header = "OPERACIÓN",
                Width = "1.2*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Monto",
                Header = "MONTO",
                Width = "0.6*",
                ColumnType = DataTableColumnType.Number,
                HorizontalAlignment = "Right",
                ShowTotal = true,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Usuario",
                Header = "USUARIO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 3
            },
            new DataTableColumn
            {
                PropertyName = "EstadoOK",
                Header = "ESTADO",
                Width = "0.5*",
                ColumnType = DataTableColumnType.Template,
                TemplateKey = "EstadoTemplate",
                HorizontalAlignment = "Center",
                DisplayPriority = 1
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

        // Configurar filtro personalizado para la tabla
        TableViewModel.CustomFilter = (registro, searchTerm) =>
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;

            var term = searchTerm.ToLower();
            return registro.Codigo?.ToLower().Contains(term) == true ||
                   registro.Placa?.ToLower().Contains(term) == true ||
                   registro.Referencia?.ToLower().Contains(term) == true ||
                   registro.NombreAgencia?.ToLower().Contains(term) == true ||
                   registro.Usuario?.ToLower().Contains(term) == true ||
                   registro.PesoNeto.ToString().Contains(term) ||
                   registro.Monto.ToString().Contains(term);
        };

        // Suscribirse a cambios en el item seleccionado
        TableViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TableViewModel.SelectedItem))
            {
                OnPropertyChanged(nameof(RegistroSeleccionado));
            }
        };

        // Configurar columnas que deben mostrar totales
        TableViewModel.ConfigureTotals(new[] { "PesoBruto", "PesoTara", "PesoNeto", "Monto" });

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
                FiltroEstado);

            // Limpiar y guardar los registros completos
            _registrosCompletos.Clear();
            foreach (var reg in registros)
            {
                _registrosCompletos[reg.baz_id] = reg;
            }

            // Mapear a DTOs para presentación
            var items = registros.Select((reg, index) => new BalanzaItemDto
            {
                Index = index + 1,
                Id = reg.baz_id,
                Codigo = $"{reg.baz_des:D5}",
                Placa = reg.baz_veh_id,
                Referencia = reg.baz_ref,
                Fecha = reg.created ?? DateTime.Now,
                PesoBruto = reg.baz_pb ?? 0,
                PesoTara = reg.baz_pt ?? 0,
                PesoNeto = reg.baz_pn ?? 0,
                Operacion = reg.ObtenerTipoOperacion(),
                Monto = reg.baz_monto,
                Usuario = reg.baz_gus_des,
                EstadoOK = reg.baz_status == 1,
                NombreAgencia = reg.baz_age_des?.ToString(),
                Estado = reg.baz_status,
                ImagenPath = reg.ObtenerNombreImagen(),
                BazPath = reg.baz_path
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
            // Obtener el registro completo del diccionario
            if (!_registrosCompletos.TryGetValue(registro.Id, out var registroCompleto))
            {
                await DialogService.ShowError("Error", "No se encontró el registro completo. Intente buscar nuevamente.");
                return;
            }
            
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

            var pdfBytes = await _balanzaReportService.GenerarReportePdfAsync(registro.Id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("Sin datos", "No se pudo generar el reporte PDF");
                return;
            }

            // Crear y abrir ventana de previsualización PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfBytes, $"Reporte {registro.Codigo}");
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

            // ImagenPath contiene "baz_media/baz_media1" (puede tener uno vacío)
            var bazMedia = string.Empty;
            var bazMedia1 = string.Empty;
            
            if (!string.IsNullOrEmpty(registro.ImagenPath))
            {
                var paths = registro.ImagenPath.Split('/');
                if (paths.Length >= 1)
                    bazMedia = paths[0];
                if (paths.Length >= 2)
                    bazMedia1 = paths[1];
            }

            // Si ambos están vacíos, no hay imágenes
            if (string.IsNullOrEmpty(bazMedia) && string.IsNullOrEmpty(bazMedia1))
            {
                LoadingService.StopLoading();
                await DialogService.ShowInfo("El registro no tiene capturas de cámara registradas", "Sin imágenes");
                return;
            }

            // Cargar imágenes de pesaje (baz_media)
            var imagenesPesaje = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia) && !string.IsNullOrEmpty(registro.BazPath))
            {
                imagenesPesaje = await _imageLoaderService.CargarImagenesAsync(
                    registro.BazPath, 
                    bazMedia);
            }

            // Cargar imágenes de destare (baz_media1)
            var imagenesDestare = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia1) && !string.IsNullOrEmpty(registro.BazPath))
            {
                imagenesDestare = await _imageLoaderService.CargarImagenesAsync(
                    registro.BazPath, 
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
                $"Registro: {registro.Codigo} - Placa: {registro.Placa}");

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
        MontoTotal = registros.Sum(r => r.Monto);
        PesoNetoPromedio = registros.Count > 0 ? registros.Average(r => r.PesoNeto) : 0;
    }
}
