using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Shared.Entities;
using Core.Services.Configuration;
using Infrastructure.Services.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// ViewModel para el mantenimiento de producción
/// Usa tipos de base de datos directamente (Pde)
/// </summary>
public partial class MantProduccionModel : ViewModelBase
{
    private readonly ISelectOptionService _selectOptionService;
    private readonly IConfigurationService _configService;
    private readonly ISerialPortService _serialPortService;

    // Propiedades de Pes (encabezado)
    [ObservableProperty] private DateTime pes_fecha = DateTime.Now;
    [ObservableProperty] private int? pes_col_id;

    // Propiedades de Pde (detalle)
    [ObservableProperty] private int pde_bie_id;
    [ObservableProperty] private int? pde_t6m_id;
    [ObservableProperty] private string? pde_nbza;
    [ObservableProperty] private float pde_pb;
    [ObservableProperty] private float pde_pt;
    [ObservableProperty] private float pde_pn;
    [ObservableProperty] private string? pde_obs;

    // Colecciones para ComboBox
    [ObservableProperty] private ObservableCollection<SelectOption> materiales = new();
    [ObservableProperty] private ObservableCollection<SelectOption> unidadesMedida = new();
    [ObservableProperty] private ObservableCollection<SelectOption> balanzas = new();
    [ObservableProperty] private ObservableCollection<SelectOption> responsables = new();

    // Propiedades de UI
    [ObservableProperty] private string? pesoB1;
    [ObservableProperty] private string? pesoB2;
    [ObservableProperty] private string? nTicket;

    // Comandos
    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand CapturarB1Command { get; }
    public ICommand CapturarB2Command { get; }

    // Patrón RequestClose para desacoplar del Window
    public Action? RequestClose { get; set; }

    public MantProduccionModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        ISelectOptionService selectOptionService,
        IConfigurationService configService,
        ISerialPortService serialPortService,
        ProduccionItemDto? item = null) : base(dialogService, loadingService)
    {
        _selectOptionService = selectOptionService;
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        
        GuardarCommand = new AsyncRelayCommand(OnGuardarAsync);
        CancelarCommand = new RelayCommand(() => RequestClose?.Invoke());
        CapturarB1Command = new AsyncRelayCommand(CapturarB1Async);
        CapturarB2Command = new AsyncRelayCommand(CapturarB2Async);
        
        _ = InicializarCombosAsync(item);
    }

    private async Task InicializarCombosAsync(ProduccionItemDto? item = null)
    {
        try
        {
            // Materiales - Asegurar que Value sea int
            var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material);
            Materiales.Clear();
            foreach (var m in mats)
            {
                var valorInt = m.Value is int intVal ? intVal : int.Parse(m.Value?.ToString() ?? "0");
                Materiales.Add(new SelectOption { Value = valorInt, Label = m.Label });
            }

            // Unidades de Medida - Asegurar que Value sea int
            var umeds = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Umedida);
            UnidadesMedida.Clear();
            foreach (var u in umeds)
            {
                var valorInt = u.Value is int intVal ? intVal : int.Parse(u.Value?.ToString() ?? "0");
                UnidadesMedida.Add(new SelectOption { Value = valorInt, Label = u.Label });
            }
            
            // Debug: Verificar que se cargaron unidades de medida
            System.Diagnostics.Debug.WriteLine($"Unidades de Medida cargadas: {UnidadesMedida.Count}");
            foreach (var um in UnidadesMedida)
            {
                System.Diagnostics.Debug.WriteLine($"  - {um.Label} (Value: {um.Value})");
            }

            // Responsables - Asegurar que Value sea int
            var resp = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Colaborador);
            Responsables.Clear();
            foreach (var r in resp)
            {
                var valorInt = r.Value is int intVal ? intVal : int.Parse(r.Value?.ToString() ?? "0");
                Responsables.Add(new SelectOption { Value = valorInt, Label = r.Label });
            }

            // Balanzas (lista simple de strings)
            Balanzas.Clear();
            var sede = _configService.GetSedeActiva();
            if (sede != null)
            {
                foreach (var balanza in sede.Balanzas)
                {
                    Balanzas.Add(new SelectOption { Value = balanza.Nombre, Label = balanza.Nombre });
                }
            }

            // Iniciar lectura de balanzas
            IniciarLecturaBalanzas();

            // Si es edición, setear valores
            if (item != null)
            {
                Pes_fecha = item.pes_fecha;
                Pde_bie_id = item.pde_bie_id;
                Pde_t6m_id = item.pde_t6m_id;
                Pes_col_id = item.pes_col_id;
                Pde_nbza = item.pde_nbza;
                Pde_pb = item.pde_pb;
                Pde_pt = item.pde_pt;
                Pde_pn = item.pde_pn;
                Pde_obs = item.pde_obs;
                
                System.Diagnostics.Debug.WriteLine($"Editando item - Pde_t6m_id: {Pde_t6m_id}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Modo nuevo - Pde_mde_id es null");
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar datos: {ex.Message}", "Error");
            System.Diagnostics.Debug.WriteLine($"Error en InicializarCombosAsync: {ex}");
        }
    }

    /// <summary>
    /// Cálculo automático de peso neto cuando cambia peso bruto
    /// </summary>
    partial void OnPde_pbChanged(float value)
    {
        Pde_pn = value - Pde_pt;
    }

    /// <summary>
    /// Cálculo automático de peso neto cuando cambia peso tara
    /// </summary>
    partial void OnPde_ptChanged(float value)
    {
        Pde_pn = Pde_pb - value;
    }

    private async Task OnGuardarAsync()
    {
        // Validación básica
        if (Pde_bie_id <= 0 || Pde_t6m_id == null || Pes_col_id == null || 
            Pde_pb <= 0 || Pde_pt < 0 || string.IsNullOrWhiteSpace(Pde_nbza))
        {
            await DialogService.ShowWarning("Complete todos los campos obligatorios.", "Validación");
            return;
        }

        // TODO: Implementar lógica de guardado
        await DialogService.ShowInfo("Guardado exitoso (pendiente implementación)", "Éxito");
    }

    private async Task CapturarB1Async()
    {
        // TODO: Implementar captura desde balanza B1-A
        if (!string.IsNullOrEmpty(PesoB1) && float.TryParse(PesoB1, out float peso))
        {
            Pde_pb = peso;
            Pde_nbza = "B1-A";
            await DialogService.ShowInfo($"Peso capturado: {peso} kg desde B1-A", "Captura");
        }
    }

    private async Task CapturarB2Async()
    {
        // TODO: Implementar captura desde balanza B2-A
        if (!string.IsNullOrEmpty(PesoB2) && float.TryParse(PesoB2, out float peso))
        {
            Pde_pb = peso;
            Pde_nbza = "B2-A";
            await DialogService.ShowInfo($"Peso capturado: {peso} kg desde B2-A", "Captura");
        }
    }
    private void IniciarLecturaBalanzas()
    {
        var sede = _configService.GetSedeActiva();
        if (sede != null && sede.Balanzas.Any())
        {
            // Iniciar servicio
            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            _serialPortService.IniciarLectura(sede.Balanzas);
        }
    }

    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        // Actualizar propiedades en el hilo de la UI
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var sede = _configService.GetSedeActiva();
            if (sede == null) return;

            foreach (var lectura in lecturas)
            {
                // Buscar qué balanza es por el puerto
                var balanza = sede.Balanzas.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanza != null)
                {
                    // Asumimos que PesoB1 es la primera balanza y PesoB2 la segunda, 
                    // o mapeamos por nombre si es posible.
                    // Por ahora mapeamos por índice para B1 y B2
                    if (sede.Balanzas.Count > 0 && balanza.Id == sede.Balanzas[0].Id) PesoB1 = lectura.Value;
                    if (sede.Balanzas.Count > 1 && balanza.Id == sede.Balanzas[1].Id) PesoB2 = lectura.Value;
                }
            }
        });
    }

    public void Cleanup()
    {
        _serialPortService.DetenerLectura();
        _serialPortService.OnPesosLeidos -= OnPesosLeidos;
    }
}
