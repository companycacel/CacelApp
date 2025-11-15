using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Shared.Entities;
using Infrastructure.Services.Balanza;
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
    public IAsyncRelayCommand EditarCommand { get; }
    public IAsyncRelayCommand EliminarCommand { get; }
    public IAsyncRelayCommand GenerarReporteCommand { get; }
    public IAsyncRelayCommand CancelarCommand { get; }
    public IAsyncRelayCommand GuardarCommand { get; }

    public BalanzaModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IBalanzaReadService balanzaReadService,
        IBalanzaWriteService balanzaWriteService,
        IBalanzaReportService balanzaReportService) : base(dialogService, loadingService)
    {
        _balanzaReadService = balanzaReadService ?? throw new ArgumentNullException(nameof(balanzaReadService));
        _balanzaWriteService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));

        // Configurar columnas de la tabla
        TableColumns = new ObservableCollection<DataTableColumn>
        {
           
            new DataTableColumn
            {
                PropertyName = "Codigo",
                Header = "CÓDIGO",
                
                ColumnType = DataTableColumnType.Text
            },
            new DataTableColumn
            {
                PropertyName = "Placa",
                Header = "PLACA",
               
                ColumnType = DataTableColumnType.Text
            },
            new DataTableColumn
            {
                PropertyName = "Referencia",
                Header = "REFERENCIA",
           
                ColumnType = DataTableColumnType.Text
            },
            new DataTableColumn
            {
                PropertyName = "Fecha",
                Header = "FECHA",
                Width = "100",
                ColumnType = DataTableColumnType.Date,
                StringFormat = "dd/MM/yyyy"
            },
            new DataTableColumn
            {
                PropertyName = "PesoBruto",
                Header = "P. BRUTO",
                Width = "110",
                ColumnType = DataTableColumnType.Number,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                ShowTotal = true
            },
            new DataTableColumn
            {
                PropertyName = "PesoTara",
                Header = "P. TARA",
                Width = "110",
                ColumnType = DataTableColumnType.Number,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                ShowTotal = true
            },
            new DataTableColumn
            {
                PropertyName = "PesoNeto",
                Header = "P. NETO",
                Width = "110",
                ColumnType = DataTableColumnType.Number,
                StringFormat = "N2",
                HorizontalAlignment = "Right",
                ShowTotal = true
            },
            new DataTableColumn
            {
                PropertyName = "NombreAgencia",
                Header = "AGENCIA",
                Width = "1.5*",
                ColumnType = DataTableColumnType.Text
            },
            new DataTableColumn
            {
                PropertyName = "Monto",
                Header = "MONTO",
                Width = "110",
                ColumnType = DataTableColumnType.Currency,
                HorizontalAlignment = "Right",
                ShowTotal = true
            },
            new DataTableColumn
            {
                PropertyName = "Usuario",
                Header = "USUARIO",
                Width = "120",
                ColumnType = DataTableColumnType.Text
            },
            new DataTableColumn
            {
                PropertyName = "EstadoOK",
                Header = "ESTADO",
                Width = "80",
                ColumnType = DataTableColumnType.Template,
                TemplateKey = "EstadoTemplate",
                HorizontalAlignment = "Center"
            },
            new DataTableColumn
            {
                PropertyName = "Acciones",
                Header = "ACCIONES",
                Width = "100",
                ColumnType = DataTableColumnType.Template,
                TemplateKey = "AccionesTemplate",
                HorizontalAlignment = "Center",
                CanSort = false
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
                EditarCommand.NotifyCanExecuteChanged();
                EliminarCommand.NotifyCanExecuteChanged();
            }
        };

        // Inicializar comandos
        BuscarCommand = new AsyncRelayCommand(BuscarRegistrosAsync);
        AgregarCommand = new AsyncRelayCommand(AgregarRegistroAsync);
        EditarCommand = new AsyncRelayCommand(EditarRegistroAsync, () => RegistroSeleccionado != null);
        EliminarCommand = new AsyncRelayCommand(EliminarRegistroAsync, () => RegistroSeleccionado != null);
        GenerarReporteCommand = new AsyncRelayCommand(GenerarReporteAsync);
        CancelarCommand = new AsyncRelayCommand(CancelarAsync);
        GuardarCommand = new AsyncRelayCommand(GuardarRegistroAsync);

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

            // Mapear a DTOs para presentación
            var items = registros.Select((reg, index) => new BalanzaItemDto
            {
                Index = index + 1,
                Codigo = $"BAZ-{reg.baz_des:D5}",
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
                ImagenPath = reg.ObtenerNombreImagen()
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
            EsEdicion = false;
            RegistroEditando = new BalanzaRegistroDto
            {
                Fecha = DateTime.Now,
                Estado = 1,
                Tipo = 0
            };

            // Aquí se puede abrir un diálogo o una ventana de edición
            await DialogService.ShowInfo("Nuevo Registro", "Abre formulario de nuevo registro");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError("Error", ex.Message);
        }
    }

    /// <summary>
    /// Abre el diálogo para editar el registro seleccionado
    /// </summary>
    private async Task EditarRegistroAsync()
    {
        if (RegistroSeleccionado == null)
        {
            await DialogService.ShowWarning("Selección requerida", "Por favor seleccione un registro para editar");
            return;
        }

        try
        {
            EsEdicion = true;
            RegistroEditando = new BalanzaRegistroDto
            {
                Id = int.Parse(RegistroSeleccionado.Codigo.Replace("BAZ-", "")),
                Placa = RegistroSeleccionado.Placa,
                Referencia = RegistroSeleccionado.Referencia,
                Fecha = RegistroSeleccionado.Fecha,
                PesoBruto = RegistroSeleccionado.PesoBruto,
                PesoTara = RegistroSeleccionado.PesoTara,
                PesoNeto = RegistroSeleccionado.PesoNeto,
                Monto = RegistroSeleccionado.Monto
            };

            await DialogService.ShowInfo("Editar Registro", "Abre formulario de edición");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError("Error", ex.Message);
        }
    }

    /// <summary>
    /// Elimina el registro seleccionado previa confirmación
    /// </summary>
    private async Task EliminarRegistroAsync()
    {
        if (RegistroSeleccionado == null)
        {
            await DialogService.ShowWarning("Selección requerida", "Por favor seleccione un registro para eliminar");
            return;
        }

        try
        {
            var confirmacion = await DialogService.ShowConfirm(
                "Confirmar eliminación",
                "¿Está seguro de que desea eliminar este registro?");

            if (!confirmacion)
                return;

            //LoadingService.Show("Eliminando registro...");

            var id = int.Parse(RegistroSeleccionado.Codigo.Replace("BAZ-", ""));
            await _balanzaWriteService.EliminarRegistroAsync(id);

            await DialogService.ShowSuccess("Éxito", "Registro eliminado correctamente");
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
    /// Guarda el registro que está siendo editado
    /// </summary>
    private async Task GuardarRegistroAsync()
    {
        if (RegistroEditando == null)
            return;

        try
        {
            //LoadingService.Show(EsEdicion ? "Actualizando registro..." : "Creando registro...");

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
    /// Genera un reporte de los registros
    /// </summary>
    private async Task GenerarReporteAsync()
    {
        try
        {
            if (!FechaInicio.HasValue || !FechaFinal.HasValue)
            {
                await DialogService.ShowWarning("Filtro requerido", "Por favor especifique el rango de fechas");
                return;
            }

            //LoadingService.Show("Generando reporte...");

            await _balanzaReportService.GenerarReporteExcelAsync(
                FechaInicio.Value,
                FechaFinal.Value,
                FiltroPlaca);

            await DialogService.ShowSuccess("Éxito", "Reporte generado correctamente");
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
    /// Actualiza las estadísticas mostradas
    /// </summary>
    private void ActualizarEstadisticas(List<BalanzaItemDto> registros)
    {
        TotalRegistros = registros.Count;
        MontoTotal = registros.Sum(r => r.Monto);
        PesoNetoPromedio = registros.Count > 0 ? registros.Average(r => r.PesoNeto) : 0;
    }
}
