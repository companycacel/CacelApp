using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Services.Image;
using CacelApp.Shared;
using CacelApp.Shared.Controls.DataTable;
using static CacelApp.Shared.Controls.DataTable.DataTableColumnBuilder;
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
using MaterialDesignThemes.Wpf;

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

        // Configurar columnas - acceso directo sin wrapper, IntelliSense completo
        TableColumns = new ObservableCollection<DataTableColumn>
        {
            new ColDef<PesajesItemDto> { Key = x => x.pes_des, Header = "CÓDIGO", Width = "0.8*", Command = VerPdfCommand, Priority = 1 },
            new ColDef<PesajesItemDto> { Key = x => x.pes_mov_des, Header = "MOVIMIENTO", Width = "1.2*", Priority = 1 },
            new ColDef<PesajesItemDto> { Key = x => x.pes_referencia, Header = "REFERENCIA", Width = "1*", Priority = 2 },
            new ColDef<PesajesItemDto> { Key = x => x.pes_fecha, Header = "FECHA", Width = "1*", Format = "dd/MM/yyyy HH:mm", Type = DataTableColumnType.Date, Priority = 1 },
            new ColDef<PesajesItemDto> { Key = x => x.pes_baz_des, Header = "BALANZA", Width = "0.8*", Command = VerBalanzaCommand, Priority = 2 },
            new ColDef<PesajesItemDto> { Key = x => x.pes_status, Header = "ESTADO", Width = "0.8*", Template = "EstadoTemplate", Align = "Center", Priority = 1 },
            new ColDef<PesajesItemDto> { Key = x => x.pes_gus_des, Header = "USUARIO", Width = "1*", Priority = 2 },
            new ColDef<PesajesItemDto> { Key = x => x.updated, Header = "ACTUALIZADO", Width = "1*", Format = "dd/MM/yyyy HH:mm", Type = DataTableColumnType.Date, Priority = 3 },
            new ColDef<PesajesItemDto>
            {
                Key = x => x.Index,
                Header = "ACCIONES",
                Width = "0.7*",
                Priority = 1,
                Actions = new List<ActionDef>
                {
                    new ActionDef { Icon = PackIconKind.Pencil, Command = EditarCommand, Tooltip = "Editar", IconSize = 24 },
                    new ActionDef { Icon = PackIconKind.Delete, Command = AnularCommand, Tooltip = "Anular", IconSize = 24 }
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

            // Mapear a DTOs para presentación - herencia directa, copiar propiedades
            var items = response.Data.Select(reg =>
            {
                // Crear DTO y copiar todas las propiedades de Pes
                var dto = new PesajesItemDto();
                ObjectMapper.CopyProperties(reg, dto);
                dto.CanEdit = _pesajesService.CanEdit(reg.pes_tipo);
                dto.CanDelete = _pesajesService.CanDelete(reg.pes_status);
                return dto;
            }).ToList();


            if (items.Any())
            {
                var firstItem = items.First();
                
            }

            // Cargar datos en la tabla reutilizable
            TableViewModel.SetData(items);

            if (TableViewModel.PaginatedData.Any())
            {
                var firstPaginated = TableViewModel.PaginatedData.First();
              
            }
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
            var response = await _pesajesService.GetPesajesById(item.pes_id);

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

            RequestClose?.Invoke();

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
            if (!_registrosCompletos.TryGetValue(item.pes_id, out var pesaje))
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
                $"Registro N° {item.pes_des} anulado");

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

            var pdfBytes = await _pesajesService.GetReportAsync(item.pes_id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("No se pudo generar el reporte", "Advertencia");
                return;
            }

            LoadingService.StopLoading();

            // Abrir visor de PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfBytes, $"Pesaje {item.pes_des}");
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
        if (item == null || item.pes_baz_id == null || item.pes_baz_id <= 0) return;

        try
        {
            LoadingService.StartLoading();

            // Obtener PDF de la balanza
            var pdfBytes = await _balanzaReportService.GenerarReportePdfAsync(item.pes_baz_id.Value);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("No se pudo generar el reporte de balanza", "Advertencia");
                return;
            }

            LoadingService.StopLoading();

            // Abrir visor de PDF
            var pdfViewer = new CacelApp.Shared.Controls.PdfViewer.PdfViewerWindow(pdfBytes, $"Balanza - {item.pes_baz_des}");
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
        RegistrosProcesados = registros.Count(r => r.pes_status == 1);
        RegistrosRegistrando = registros.Count(r => r.pes_status == 2);
    }

    public Action? RequestClose { get; set; }
}
