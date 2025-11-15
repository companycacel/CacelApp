using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
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

    // Propiedades de Datos

    [ObservableProperty]
    private BalanzaItemDto? selectedRegistro;

    [ObservableProperty]
    private int totalRegistros;

    [ObservableProperty]
    private decimal montoTotal;

    [ObservableProperty]
    private decimal pesoNetoPromedio;

    [ObservableProperty]
    private bool esEdicion;

    [ObservableProperty]
    private BalanzaRegistroDto? registroEditando;

    [ObservableProperty]
    private ObservableCollection<BalanzaItemDto> _registrosPaginated = new();

    // Paginación
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 25; // Default items per page
    public List<int> PageSizes { get; } = new List<int> { 10, 25, 50, 100 };

    [ObservableProperty]
    private int _totalPages = 1;

    // Búsqueda rápida (Global Search)
    [ObservableProperty]
    private string? _globalSearchTerm;

    // Colección completa de registros (privada)
    private ObservableCollection<BalanzaItemDto> _allRegistros = new();

    // Comandos de Paginación
    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void PreviousPage() => CurrentPage--;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void NextPage() => CurrentPage++;

    // Propiedades de estado de paginación
    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages && TotalPages > 0;

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

        // Inicializar comandos
        BuscarCommand = new AsyncRelayCommand(BuscarRegistrosAsync);
        AgregarCommand = new AsyncRelayCommand(AgregarRegistroAsync);
        EditarCommand = new AsyncRelayCommand(EditarRegistroAsync);
        EliminarCommand = new AsyncRelayCommand(EliminarRegistroAsync);
        GenerarReporteCommand = new AsyncRelayCommand(GenerarReporteAsync);
        CancelarCommand = new AsyncRelayCommand(CancelarAsync);
        GuardarCommand = new AsyncRelayCommand(GuardarRegistroAsync);
        // Cargar datos iniciales
        _ = BuscarRegistrosAsync();
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1; 
        ApplyFilteringAndPaging();
    }

    partial void OnCurrentPageChanged(int value)
    {
        ApplyFilteringAndPaging();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnGlobalSearchTermChanged(string? value)
    {
        CurrentPage = 1;
        ApplyFilteringAndPaging();
    }


    /// <summary>
    /// Aplica la búsqueda global y la paginación a los registros
    /// </summary>
    private void ApplyFilteringAndPaging()
    {
        var filteredList = _allRegistros.AsEnumerable();

        // 1. Aplicar Búsqueda Global (Global Search)
        if (!string.IsNullOrWhiteSpace(GlobalSearchTerm))
        {
            var term = GlobalSearchTerm.Trim().ToLower();
            // Busca en propiedades relevantes
            filteredList = filteredList.Where(r =>
                r.Placa.ToLower().Contains(term) ||
                r.Referencia.ToLower().Contains(term) ||
                r.Usuario.ToLower().Contains(term) ||
                r.Codigo.ToLower().Contains(term));
        }

        // 2. Calcular Paginación
        TotalRegistros = filteredList.Count();
        TotalPages = (int)Math.Ceiling((double)TotalRegistros / PageSize);

        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages > 0 ? TotalPages : 1;
        // 3. Aplicar Paginación
        var orderedList = filteredList.ToList();
        var paginatedList = orderedList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        int startIndex = (CurrentPage - 1) * PageSize;
        for (int i = 0; i < paginatedList.Count; i++)
        {
            paginatedList[i].Index = startIndex + i + 1; // 1, 2, 3...
        }
        // 4. Actualizar la colección observable de la UI
        RegistrosPaginated = new ObservableCollection<BalanzaItemDto>(paginatedList);

        ActualizarEstadisticas(filteredList.ToList()); // Actualizar stats con la lista filtrada
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
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
            var items = new ObservableCollection<BalanzaItemDto>();
            foreach (var reg in registros)
            {
                items.Add(new BalanzaItemDto
                {
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
                    ImagenPath =reg.ObtenerNombreImagen()
                });
            }
            _allRegistros = items;
            ApplyFilteringAndPaging();

    
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message,"Error al buscar registros");
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
        if (SelectedRegistro == null)
        {
            await DialogService.ShowWarning("Selección requerida", "Por favor seleccione un registro para editar");
            return;
        }

        try
        {
            EsEdicion = true;
            RegistroEditando = new BalanzaRegistroDto
            {
                Id = int.Parse(SelectedRegistro.Codigo.Replace("BAZ-", "")),
                Placa = SelectedRegistro.Placa,
                Referencia = SelectedRegistro.Referencia,
                Fecha = SelectedRegistro.Fecha,
                PesoBruto = SelectedRegistro.PesoBruto,
                PesoTara = SelectedRegistro.PesoTara,
                PesoNeto = SelectedRegistro.PesoNeto,
                Monto = SelectedRegistro.Monto
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
        if (SelectedRegistro == null)
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

            var id = int.Parse(SelectedRegistro.Codigo.Replace("BAZ-", ""));
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
    private void ActualizarEstadisticas(List<BalanzaItemDto> filteredList)
    {
        MontoTotal = filteredList.Sum(r => r.Monto);
        PesoNetoPromedio = filteredList.Count > 0 ? filteredList.Average(r => r.PesoNeto) : 0;
    }
}
