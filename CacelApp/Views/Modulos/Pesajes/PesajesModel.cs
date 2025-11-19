using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Services.Image;
using CacelApp.Shared;
using CacelApp.Shared.Controls;
using CacelApp.Shared.Controls.DataTable;
using CacelApp.Shared.Entities;
using Infrastructure.Services.Balanza;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Pesajes;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Pesajes;

/// <summary>
/// ViewModel para el módulo de Pesajes
/// Implementa MVVM pattern con MVVM Community Toolkit
/// Gestiona el listado de pesajes con filtros por tipo
/// </summary>
public partial class PesajesModel : ViewModelBase
{
    private readonly IPesajesService _pesajesService;
    private readonly IBalanzaReportService _balanzaReportService;
    private readonly Infrastructure.Services.Shared.ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    
    // Diccionario para guardar los registros completos
    private readonly Dictionary<int, Pes> _registrosCompletos = new();

    #region Propiedades Observables

    // Tipo de pesaje seleccionado (PE, PS, DS)
    [ObservableProperty]
    private string tipoSeleccionado = "PE";

    partial void OnTipoSeleccionadoChanged(string value)
    {
        _ = CargarPesajesAsync();
    }

    #region DataTable Reutilizable

    /// <summary>
    /// ViewModel de la tabla reutilizable
    /// </summary>
    public DataTableViewModel<PesajesItemDto> TableViewModel { get; } = new();

    /// <summary>
    /// Configuración de columnas para la tabla
    /// </summary>
    public ObservableCollection<DataTableColumn> TableColumns { get; }

    /// <summary>
    /// Acceso al registro seleccionado desde la tabla
    /// </summary>
    public PesajesItemDto? RegistroSeleccionado => TableViewModel.SelectedItem?.Item;

    #endregion

    // Estadísticas
    [ObservableProperty]
    private int totalRegistros;

    [ObservableProperty]
    private int registrosProcesados;

    [ObservableProperty]
    private int registrosRegistrando;

    #endregion

    #region Comandos

    public IAsyncRelayCommand CargarCommand { get; }
    public IAsyncRelayCommand<PesajesItemDto> EditarCommand { get; }
    public IAsyncRelayCommand<PesajesItemDto> AnularCommand { get; }
    public IAsyncRelayCommand<PesajesItemDto> VerPdfCommand { get; }
    public IAsyncRelayCommand<PesajesItemDto> VerBalanzaCommand { get; }

    #endregion

    public PesajesModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IPesajesService pesajesService,
        IBalanzaReportService balanzaReportService,
        Infrastructure.Services.Shared.ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService) : base(dialogService, loadingService)
    {
        _pesajesService = pesajesService ?? throw new ArgumentNullException(nameof(pesajesService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));

        // Inicializar comandos
        CargarCommand = new AsyncRelayCommand(CargarPesajesAsync);
        EditarCommand = new AsyncRelayCommand<PesajesItemDto>(EditarPesajeAsync);
        AnularCommand = new AsyncRelayCommand<PesajesItemDto>(AnularPesajeAsync);
        VerPdfCommand = new AsyncRelayCommand<PesajesItemDto>(VerPdfAsync);
        VerBalanzaCommand = new AsyncRelayCommand<PesajesItemDto>(VerBalanzaAsync);

        // Configurar columnas de la tabla
        TableColumns = new ObservableCollection<DataTableColumn>
        {
            new DataTableColumn
            {
                PropertyName = "Pes_des",
                Header = "CÓDIGO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Hyperlink,
                HyperlinkCommand = VerPdfCommand,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pes_mov_des",
                Header = "MOVIMIENTO",
                Width = "1.2*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pes_referencia",
                Header = "REFERENCIA",
                Width = "1*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Pes_fecha",
                Header = "FECHA",
                Width = "1*",
                ColumnType = DataTableColumnType.Date,
                StringFormat = "dd/MM/yyyy HH:mm",
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pes_baz_des",
                Header = "BALANZA",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Hyperlink,
                HyperlinkCommand = VerBalanzaCommand,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Pes_status_des",
                Header = "ESTADO",
                Width = "0.8*",
                ColumnType = DataTableColumnType.Template,
                TemplateKey = "EstadoTemplate",
                HorizontalAlignment = "Center",
                DisplayPriority = 1
            },
            new DataTableColumn
            {
                PropertyName = "Pes_gus_des",
                Header = "USUARIO",
                Width = "1*",
                ColumnType = DataTableColumnType.Text,
                DisplayPriority = 2
            },
            new DataTableColumn
            {
                PropertyName = "Updated",
                Header = "ACTUALIZADO",
                Width = "1*",
                ColumnType = DataTableColumnType.Date,
                StringFormat = "dd/MM/yyyy HH:mm",
                DisplayPriority = 3
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
                        Icon = MaterialDesignThemes.Wpf.PackIconKind.Cancel,
                        Tooltip = "Anular",
                        Command = AnularCommand,
                        IconSize = 24
                    }
                }
            }
        };
    }

    /// <summary>
    /// Carga los pesajes según el tipo seleccionado
    /// </summary>
    private async Task CargarPesajesAsync()
    {
        try
        {
            LoadingService.StartLoading();

            var response = await _pesajesService.GetPesajes(TipoSeleccionado);

            // Debug: Ver respuesta
            System.Diagnostics.Debug.WriteLine($"[Pesajes] Response status: {response.status}");
            System.Diagnostics.Debug.WriteLine($"[Pesajes] Data count: {response.Data?.Count() ?? 0}");

            if (response.status != 1 || response.Data == null)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "Error al cargar pesajes", "Error");
                return;
            }

            // Limpiar y guardar los registros completos
            _registrosCompletos.Clear();
            foreach (var reg in response.Data)
            {
                _registrosCompletos[reg.pes_id] = reg;
            }

            // Mapear a DTOs para presentación
            var items = response.Data.Select(reg => new PesajesItemDto
            {
                Pes_id = reg.pes_id,
                Pes_des = reg.pes_des,
                Pes_mov_des = reg.pes_mov_des,
                Pes_referencia = reg.pes_referencia,
                Pes_fecha = reg.pes_fecha,
                Pes_baz_des = reg.pes_baz_des,
                Pes_status = reg.pes_status,
                Pes_status_des = _pesajesService.GetStatusDescription(reg.pes_status),
                Pes_gus_des = reg.pes_gus_des,
                Updated = reg.updated,
                Pes_tipo = reg.pes_tipo,
                Pes_baz_id = reg.pes_baz_id,
                CanEdit = _pesajesService.CanEdit(reg.pes_tipo),
                CanDelete = _pesajesService.CanDelete(reg.pes_status)
            }).ToList();

            // Debug: Ver items mapeados
            System.Diagnostics.Debug.WriteLine($"[Pesajes] Items mapeados: {items.Count}");
            if (items.Any())
            {
                var firstItem = items.First();
                System.Diagnostics.Debug.WriteLine($"[Pesajes] Primer item - Código: {firstItem.Pes_des}, Movimiento: {firstItem.Pes_mov_des}");
            }

            // Cargar datos en la tabla reutilizable
            TableViewModel.SetData(items);
            
            // Debug: Ver datos en TableViewModel
            System.Diagnostics.Debug.WriteLine($"[Pesajes] TableViewModel.TotalRecords: {TableViewModel.TotalRecords}");
            System.Diagnostics.Debug.WriteLine($"[Pesajes] TableViewModel.PaginatedData.Count: {TableViewModel.PaginatedData.Count}");
            if (TableViewModel.PaginatedData.Any())
            {
                var firstPaginated = TableViewModel.PaginatedData.First();
                System.Diagnostics.Debug.WriteLine($"[Pesajes] Primer item paginado - RowNumber: {firstPaginated.RowNumber}, Item.Pes_des: {firstPaginated.Item?.Pes_des}");
            }

            // Debug: Ver datos en TableViewModel
            System.Diagnostics.Debug.WriteLine($"[Pesajes] TableViewModel.TotalRecords: {TableViewModel.TotalRecords}");
            System.Diagnostics.Debug.WriteLine($"[Pesajes] TableViewModel.PaginatedData.Count: {TableViewModel.PaginatedData.Count}");

            // Actualizar estadísticas
            ActualizarEstadisticas(items);
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al cargar pesajes");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Edita un pesaje existente
    /// </summary>
    private async Task EditarPesajeAsync(PesajesItemDto? item)
    {
        if (item == null) return;

        try
        {
            LoadingService.StartLoading();

            // Obtener el registro completo con todos sus detalles
            var response = await _pesajesService.GetPesajesById(item.Pes_id);

            if (response.status != 1 || response.Data == null)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "No se pudo cargar el pesaje", "Error");
                return;
            }

            LoadingService.StopLoading();

            // Crear ViewModel para el mantenimiento
            var viewModel = new MantPesajesModel(
                DialogService,
                LoadingService,
                _pesajesService,
                _selectOptionService,
                _imageLoaderService);

            // Inicializar con el pesaje existente
            await viewModel.InicializarAsync(response.Data);

            // Abrir ventana
            var ventana = new MantPesajes
            {
                DataContext = viewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            viewModel.RequestClose = () => ventana.Close();

            var resultado = ventana.ShowDialog();

            // Recargar listado si se guardaron cambios
            if (resultado == true)
            {
                await CargarPesajesAsync();
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al editar pesaje");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Anula un pesaje
    /// </summary>
    private async Task AnularPesajeAsync(PesajesItemDto? item)
    {
        if (item == null || !item.CanDelete) return;

        var confirmar = await DialogService.ShowConfirm(
            "¿Está seguro de anular el registro?",
            "Anular Registro");

        if (!confirmar) return;

        try
        {
            LoadingService.StartLoading();

            // Obtener el registro completo
            if (!_registrosCompletos.TryGetValue(item.Pes_id, out var pesaje))
            {
                await DialogService.ShowError("No se encontró el registro", "Error");
                return;
            }

            pesaje.action = ActionType.Delete.ToString();
            var response = await _pesajesService.Pesajes(pesaje);

            if (response.status != 1)
            {
                await DialogService.ShowError(response.Meta.msg, "Error al anular");
                return;
            }

            await DialogService.ShowSuccess(
                response.Meta.msg,
                $"Registro N° {item.Pes_des} anulado");

            // Recargar listado
            await CargarPesajesAsync();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al anular pesaje");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Ver el PDF del pesaje
    /// </summary>
    private async Task VerPdfAsync(PesajesItemDto? item)
    {
        if (item == null) return;

        try
        {
            LoadingService.StartLoading();

            var pdfBytes = await _pesajesService.GetReportAsync(item.Pes_id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("No se pudo generar el reporte", "Advertencia");
                return;
            }

            LoadingService.StopLoading();

            // Abrir visor de PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewerWindow(pdfBytes, $"Pesaje {item.Pes_des}");
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
    /// Ver el PDF de la balanza asociada
    /// </summary>
    private async Task VerBalanzaAsync(PesajesItemDto? item)
    {
        if (item == null || item.Pes_baz_id == null || item.Pes_baz_id <= 0) return;

        try
        {
            LoadingService.StartLoading();

            // Obtener PDF de la balanza
            var pdfBytes = await _balanzaReportService.GenerarReportePdfAsync(item.Pes_baz_id.Value);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("No se pudo generar el reporte de balanza", "Advertencia");
                return;
            }

            LoadingService.StopLoading();

            // Abrir visor de PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewerWindow(pdfBytes, $"Balanza - {item.Pes_baz_des}");
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
    /// Actualiza las estadísticas mostradas
    /// </summary>
    private void ActualizarEstadisticas(List<PesajesItemDto> registros)
    {
        TotalRegistros = registros.Count;
        RegistrosProcesados = registros.Count(r => r.Pes_status == 1);
        RegistrosRegistrando = registros.Count(r => r.Pes_status == 2);
    }
}
