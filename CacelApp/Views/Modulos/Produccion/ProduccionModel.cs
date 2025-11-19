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
using Core.Repositories.Produccion;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// ViewModel para el módulo de Producción
/// Implementa MVVM pattern con MVVM Community Toolkit
/// Gestiona el listado de registros de producción
/// </summary>
public partial class ProduccionModel : ViewModelBase
{
    private readonly IProduccionService _produccionService;
    private readonly IImageLoaderService _imageLoaderService;
    private readonly Infrastructure.Services.Shared.ISelectOptionService _selectOptionService;
    
    // Diccionario para guardar los registros completos
    private readonly Dictionary<int, Pde> _registrosCompletos = new();

    #region Propiedades Observables

    // Filtros
    [ObservableProperty]
    private DateTime fechaInicio = DateTime.Now.AddMonths(-1);

    [ObservableProperty]
    private DateTime fechaFin = DateTime.Now;

    [ObservableProperty]
    private ObservableCollection<SelectOption> materiales = new();

    [ObservableProperty]
    private SelectOption? materialSeleccionado;

    #region DataTable Reutilizable

    /// <summary>
    /// ViewModel de la tabla reutilizable
    /// </summary>
    public DataTableViewModel<ProduccionItemDto> TableViewModel { get; } = new();

    /// <summary>
    /// Configuración de columnas para la tabla
    /// </summary>
    public ObservableCollection<DataTableColumn> TableColumns { get; }

    /// <summary>
    /// Acceso al registro seleccionado desde la tabla
    /// </summary>
    public ProduccionItemDto? RegistroSeleccionado => TableViewModel.SelectedItem?.Item;

    #endregion

    // Estadísticas
    [ObservableProperty]
    private int totalRegistros;

    #endregion

    #region Comandos

    public IAsyncRelayCommand BuscarCommand { get; }
    public IAsyncRelayCommand AgregarCommand { get; }
    public IAsyncRelayCommand CargarCommand { get; }
    public IAsyncRelayCommand<ProduccionItemDto> EditarCommand { get; }
    public IAsyncRelayCommand<ProduccionItemDto> EliminarCommand { get; }
    public IAsyncRelayCommand<ProduccionItemDto> VerPdfCommand { get; }
    public IAsyncRelayCommand<ProduccionItemDto> VerImagenesCommand { get; }

    #endregion

    public ProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IProduccionService produccionService,
        IImageLoaderService imageLoaderService,
        Infrastructure.Services.Shared.ISelectOptionService selectOptionService) : base(dialogService, loadingService)
    {
        _produccionService = produccionService ?? throw new ArgumentNullException(nameof(produccionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));

        // Inicializar comandos
        BuscarCommand = new AsyncRelayCommand(CargarProduccionAsync);
        AgregarCommand = new AsyncRelayCommand(AgregarProduccionAsync);
        CargarCommand = new AsyncRelayCommand(CargarProduccionAsync);
        EditarCommand = new AsyncRelayCommand<ProduccionItemDto>(EditarProduccionAsync);
        EliminarCommand = new AsyncRelayCommand<ProduccionItemDto>(EliminarProduccionAsync);
        VerPdfCommand = new AsyncRelayCommand<ProduccionItemDto>(VerPdfAsync);
        VerImagenesCommand = new AsyncRelayCommand<ProduccionItemDto>(VerImagenesAsync);

        // Configurar columnas de la tabla
        TableColumns = new ObservableCollection<DataTableColumn>
        {
            new DataTableColumn
            {
                PropertyName = "Pde_pes_des",
                Header = "PESAJE",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Hyperlink,
                HyperlinkCommand = VerPdfCommand,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pde_pes_fecha",
                Header = "FECHA",
                Width = "1*",
                ColumnType = DataTableColumnType.Date,
                StringFormat = "dd/MM/yyyy HH:mm",
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pde_bie_des",
                Header = "MATERIAL",
                Width = "1.2*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pde_mde_des",
                Header = "MEDIDA",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Pde_nbza",
                Header = "BALANZA",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Pde_pb",
                Header = "P. BRUTO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Pde_pt",
                Header = "P. TARA",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Pde_pn",
                Header = "P. NETO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Text,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pde_gus_des",
                Header = "RESPONSABLE",
                Width = "1*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pde_obs",
                Header = "OBSERVACIONES",
                Width = "1.2*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
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
                        IconSize = 24
                    },
                    new DataTableActionButton
                    {
                        Icon = MaterialDesignThemes.Wpf.PackIconKind.Image,
                        Tooltip = "Ver Imágenes",
                        Command = VerImagenesCommand,
                        IconSize = 24
                    },
                    new DataTableActionButton
                    {
                        Icon = MaterialDesignThemes.Wpf.PackIconKind.Delete,
                        Tooltip = "Eliminar",
                        Command = EliminarCommand,
                        IconSize = 24
                    }
                }
            }
        };

        // Cargar materiales
        _ = CargarMaterialesAsync();
    }

    /// <summary>
    /// Carga los materiales disponibles
    /// </summary>
    private async Task CargarMaterialesAsync()
    {
        try
        {
            var materiales = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material);
            
            Materiales.Clear();
            Materiales.Add(new SelectOption { Value = -1, Label = "TODOS" });
            foreach (var item in materiales)
            {
                Materiales.Add(item);
            }
            MaterialSeleccionado = Materiales.FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando materiales: {ex.Message}");
        }
    }

    /// <summary>
    /// Carga los registros de producción
    /// </summary>
    private async Task CargarProduccionAsync()
    {
        try
        {
            LoadingService.StartLoading();

            var materialId = MaterialSeleccionado?.Value;
            var response = await _produccionService.GetProduccion(
                FechaInicio, 
                FechaFin, 
                materialId > 0 ? materialId : null);

            System.Diagnostics.Debug.WriteLine($"[Produccion] Response status: {response.status}");
            System.Diagnostics.Debug.WriteLine($"[Produccion] Data count: {response.Data?.Count() ?? 0}");

            if (response.status != 1 || response.Data == null)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "Error al cargar producción", "Error");
                return;
            }

            // Limpiar y guardar los registros completos
            _registrosCompletos.Clear();
            foreach (var reg in response.Data)
            {
                _registrosCompletos[reg.pde_id] = reg;
            }

            // Mapear a DTOs para presentación
            var items = response.Data.Select(reg => new ProduccionItemDto
            {
                Pde_id = reg.pde_id,
                Pde_pes_id = reg.pde_pes_id,
                Pde_pes_des = reg.pde_pes_des ?? $"PES-{reg.pde_pes_id:D6}",
                Pde_pes_fecha = reg.pes_fecha,
                Pde_bie_des = reg.pde_bie_des ?? reg.pde_bie_cod,
                Pde_mde_des = reg.pde_t6m_des ?? "",
                Pde_nbza = reg.pde_nbza ?? "",
                Pde_pb = (decimal)reg.pde_pb,
                Pde_pt = (decimal)reg.pde_pt,
                Pde_pn = (decimal)reg.pde_pn,
                Pde_gus_des = reg.pes_col_des ?? "",
                Pde_obs = reg.pde_obs ?? reg.pes_obs ?? "",
                Pde_path = reg.pde_path ?? "",
                Pde_media = reg.pde_media ?? "",
                Pde_status = 1,
                Pde_status_des = "Activo",
                CanEdit = true,
                CanDelete = false,
                HasMedia = !string.IsNullOrEmpty(reg.pde_media)
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"[Produccion] Items mapeados: {items.Count}");

            // Cargar datos en la tabla reutilizable
            TableViewModel.SetData(items);
            
            // Actualizar estadísticas
            TotalRegistros = items.Count;
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al cargar producción");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Edita un registro de producción existente
    /// </summary>
    private async Task EditarProduccionAsync(ProduccionItemDto? item)
    {
        if (item == null) return;

        try
        {
            // TODO: Implementar edición con dialog MantProduccion
            await DialogService.ShowInfo("Función en desarrollo", "Información");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al editar");
        }
    }

    /// <summary>
    /// Agrega un nuevo registro de producción
    /// </summary>
    private async Task AgregarProduccionAsync()
    {
        try
        {
            // TODO: Implementar diálogo MantProduccion para nuevo registro
            await DialogService.ShowInfo("Función en desarrollo", "Información");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al agregar");
        }
    }

    /// <summary>
    /// Elimina un registro de producción
    /// </summary>
    private async Task EliminarProduccionAsync(ProduccionItemDto? item)
    {
        if (item == null) return;

        try
        {
            var confirm = await DialogService.ShowConfirm(
                "¿Está seguro de eliminar este registro?",
                "Confirmar eliminación");

            if (!confirm)
                return;

            LoadingService.StartLoading();

            // Obtener registro completo
            if (!_registrosCompletos.TryGetValue(item.Pde_id, out var registro))
            {
                await DialogService.ShowWarning("No se encontró el registro", "Advertencia");
                return;
            }

            // TODO: Implementar lógica de eliminación
            await DialogService.ShowInfo("Función en desarrollo", "Información");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al eliminar");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Ver PDF del pesaje asociado
    /// </summary>
    private async Task VerPdfAsync(ProduccionItemDto? item)
    {
        if (item == null) return;

        try
        {
            LoadingService.StartLoading();

            var pdfData = await _produccionService.GetReportAsync(item.Pde_pes_id);
            
            if (pdfData == null || pdfData.Length == 0)
            {
                await DialogService.ShowWarning("No se pudo generar el PDF", "Advertencia");
                return;
            }

            LoadingService.StopLoading();

            // Abrir visor de PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfData, $"Producción - Pesaje {item.Pde_pes_des}");
            pdfViewer.Show();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al generar PDF");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Ver imágenes del registro de producción
    /// </summary>
    private async Task VerImagenesAsync(ProduccionItemDto? item)
    {
        if (item == null) return;

        try
        {
            if (!item.HasMedia)
            {
                await DialogService.ShowInfo("Este registro no tiene imágenes", "Información");
                return;
            }

            LoadingService.StartLoading();

            // Obtener registro completo con path y media
            if (!_registrosCompletos.TryGetValue(item.Pde_id, out var registro))
            {
                await DialogService.ShowWarning("No se encontró el registro", "Advertencia");
                return;
            }

            if (string.IsNullOrEmpty(registro.pde_path) || string.IsNullOrEmpty(registro.pde_media))
            {
                await DialogService.ShowWarning("El registro no tiene imágenes asociadas", "Advertencia");
                return;
            }

            // Cargar imágenes desde el servidor FTP
            var imagenes = await _imageLoaderService.CargarImagenesAsync(
                registro.pde_path, 
                registro.pde_media);

            LoadingService.StopLoading();

            if (!imagenes.Any())
            {
                await DialogService.ShowWarning("No se pudieron cargar las imágenes", "Sin imágenes");
                return;
            }

            // Crear ViewModel y mostrar ventana
            var viewModel = new ImageViewerViewModel(
                imagenes, 
                null,
                $"Producción - {item.Pde_pes_des} - {item.Pde_bie_des}");

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
}
