using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.DataTable;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Balanza.Entities;
using Infrastructure.Services.Balanza;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Balanza
{
    public partial class DestareVehiculosModel : ViewModelBase
    {
        private readonly IBalanzaSearchService _balanzaReadService;
        private readonly Dictionary<int, Baz> _registrosCompletos = new();

        [ObservableProperty]
        private string _filtroTexto = string.Empty;

        [ObservableProperty]
        private DateTime _fechaFiltro = DateTime.Now;

        [ObservableProperty]
        private bool _mostrarTodos = false;

        [ObservableProperty]
        private Baz? _registroSeleccionado;

        public DataTableViewModel<BalanzaItemDto> TableViewModel { get; }

        public ObservableCollection<DataTableColumn> ColumnasDestare { get; }

        public DestareVehiculosModel(
            IDialogService dialogService,
            ILoadingService loadingService,
            IBalanzaSearchService balanzaReadService) : base(dialogService, loadingService)
        {
            _balanzaReadService = balanzaReadService ?? throw new ArgumentNullException(nameof(balanzaReadService));
            // Inicializar comandos
            SeleccionarCommand = new RelayCommand<BalanzaItemDto>(SeleccionarRegistro);
            CerrarCommand = new RelayCommand(Cerrar);
            // Inicializar TableViewModel
            TableViewModel = new DataTableViewModel<BalanzaItemDto>();

            // Definir columnas para el DataTable
            ColumnasDestare = new ObservableCollection<DataTableColumn>
            {
                new ColDef<BalanzaItemDto>{ Key=x=>x.baz_des, Header="TICKET", Width="1*", Priority=1 },
                new ColDef<BalanzaItemDto>{ Key=x=>x.baz_veh_id, Header="PLACA", Width="0.8*", Priority=1 },
                new ColDef<BalanzaItemDto>{ Key=x=>x.baz_fecha, Header="FECHA", Width="1*", Type=DataTableColumnType.Date, Format="dd/MM/yyyy HH:mm", Priority=1 },
                new ColDef<BalanzaItemDto>{ Key=x=>x.baz_pb, Header="PESO BRUTO (kg)", Width="1*", Type=DataTableColumnType.Number, Format="N2", Align="Right", Priority=1 },
                new ColDef<BalanzaItemDto>{ Key=x=>x.baz_ref, Header="REFERENCIA", Width="1.5*", Priority=2 },
                new ColDef<BalanzaItemDto>
                {
                    Key=x=>x.Index, Header="SELECCIONAR", Width="0.8*", Priority=1,
                    Actions = new List<ActionDef>
                    {
                        new ActionDef{ Icon=PackIconKind.Check, Tooltip="Seleccionar", Command=SeleccionarCommand, IconSize=24 }
                    }
                }
            };

        
        }

        public IRelayCommand<BalanzaItemDto> SeleccionarCommand { get; }
        public IRelayCommand CerrarCommand { get; }

        public async Task CargarRegistrosAsync()
        {
            try
            {
                LoadingService.StartLoading();

                // Cargar registros de los últimos 3 días con status 1 (primera captura)
                var fechaInicio = DateTime.Now.AddDays(-3);
                var fechaFin = DateTime.Now;

                // Función de obtención de datos
                Func<Task<IEnumerable<Baz>>> dataFetcher = () => _balanzaReadService.ObtenerRegistrosAsync(
                    fechaInicio,
                    fechaFin,
                    null, // Sin filtro de placa
                    null, // Sin filtro de cliente
                    1     // Solo status 1 (primera captura)
                );

                // Función de mapeo
                Func<Baz, BalanzaItemDto> dtoMapper = (reg) =>
                {
                    var dto = new BalanzaItemDto();
                    ObjectMapper.CopyProperties(reg, dto);
                    return dto;
                };

                // Función para extraer ID
                Func<Baz, int> idExtractor = (reg) => reg.baz_id;

                // Ejecutar carga centralizada
                await ExecuteDataLoadAsync(
                    dataFetcher,
                    dtoMapper,
                    idExtractor,
                    _registrosCompletos,
                    TableViewModel,
                    null,
                    "Error al cargar registros");

            }
            finally
            {
                LoadingService.StopLoading();
            }
        }

        partial void OnFiltroTextoChanged(string value)
        {
            AplicarFiltro();
        }

        partial void OnFechaFiltroChanged(DateTime value)
        {
            AplicarFiltro();
        }

        partial void OnMostrarTodosChanged(bool value)
        {
            AplicarFiltro();
        }

        private void AplicarFiltro()
        {
            if (TableViewModel == null) return;

            // El DataTableViewModel ya maneja el filtrado internamente
            // Solo necesitamos actualizar el SearchTerm si hay filtro de texto
            if (!string.IsNullOrWhiteSpace(FiltroTexto))
            {
                TableViewModel.SearchTerm = FiltroTexto;
            }
            else
            {
                TableViewModel.SearchTerm = string.Empty;
            }

            // Para filtrar por fecha, usamos CustomFilter
            if (!MostrarTodos && FechaFiltro != default)
            {
                var fechaStr = FechaFiltro.ToString("dd/MM/yyyy");
                TableViewModel.CustomFilter = (item, searchTerm) =>
                {
                    if (item is BalanzaItemDto dto)
                    {
                        var cumpleFecha = dto.baz_fecha?.ToString("dd/MM/yyyy") == fechaStr;
                        var cumpleTexto = string.IsNullOrWhiteSpace(searchTerm) ||
                                         dto.baz_veh_id?.ToLower().Contains(searchTerm.ToLower()) == true;
                        return cumpleFecha && cumpleTexto;
                    }
                    return false;
                };
            }
            else
            {
                // Sin filtro de fecha, solo filtro de texto por defecto
                TableViewModel.CustomFilter = null;
            }

            TableViewModel.Refresh();
        }

        private void SeleccionarRegistro(BalanzaItemDto? item)
        {
            if (item != null && _registrosCompletos.TryGetValue(item.baz_id, out var registroCompleto))
            {
                RegistroSeleccionado = registroCompleto;
            }
        }

        private void Cerrar()
        {
            // El code-behind manejará el cierre
        }
    }
}
