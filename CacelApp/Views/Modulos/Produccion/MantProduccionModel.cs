using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Shared.Entities;
using Infrastructure.Services.Shared;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CacelApp.Views.Modulos.Produccion;

public partial class MantProduccionModel :  ViewModelBase
{
    private readonly ISelectOptionService _selectOptionService;
    private Window _window;
    public void SetWindow(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }
    [ObservableProperty] private DateTime? fecha = DateTime.Now;
    [ObservableProperty] private ObservableCollection<SelectOption> materiales = new();
    [ObservableProperty] private int? materialId;
    [ObservableProperty] private ObservableCollection<SelectOption> unidadesMedida = new();
    [ObservableProperty] private int? unidadMedidaId;
    [ObservableProperty] private ObservableCollection<SelectOption> balanzas = new();
    [ObservableProperty] private string? balanzaSeleccionada;
    [ObservableProperty] private ObservableCollection<SelectOption> responsables = new();
    [ObservableProperty] private int? responsableId;
    [ObservableProperty] private string pesoBruto;
    [ObservableProperty] private string pesoTara;
    [ObservableProperty] private string pesoNeto;
    [ObservableProperty] private string observacion;

    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    public MantProduccionModel(IDialogService dialogService,
        ILoadingService loadingService,
        ISelectOptionService selectOptionService,
        ProduccionItemDto? item = null) : base(dialogService, loadingService)
    {
        _window = null!;
        _selectOptionService = selectOptionService;
        GuardarCommand = new AsyncRelayCommand(OnGuardarAsync);
        CancelarCommand = new RelayCommand(() => _window.Close());
        _ = InicializarCombosAsync(item);
    }

    private async Task InicializarCombosAsync(ProduccionItemDto? item = null)
    {
        // Materiales
        var mats = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Material);
        Materiales.Clear();
        foreach (var m in mats) Materiales.Add(m);

        // Unidades de Medida
        var umeds = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Umedida);
        UnidadesMedida.Clear();
        foreach (var u in umeds) UnidadesMedida.Add(u);

        // Responsables
        var resp = await _selectOptionService.GetSelectOptionsAsync(Core.Shared.Enums.SelectOptionType.Colaborador);
        Responsables.Clear();
        foreach (var r in resp) Responsables.Add(r);

        // Balanzas (puede requerir lógica especial, aquí ejemplo simple)
        Balanzas.Clear();
        Balanzas.Add(new SelectOption { Value = "B1-A", Label = "B1-A" });
        Balanzas.Add(new SelectOption { Value = "B2-A", Label = "B2-A" });
        Balanzas.Add(new SelectOption { Value = "B5-O", Label = "B5-O" });

        // Si es edición, setear valores
        if (item != null)
        {
            Fecha = item.pes_fecha;
            MaterialId = item.pde_bie_id;
            UnidadMedidaId = item.pde_mde_id;
            ResponsableId = item.pes_col_id;
            BalanzaSeleccionada = item.pde_nbza;
            PesoBruto = item.pde_pb.ToString();
            PesoTara = item.pde_pt.ToString();
            PesoNeto = item.pde_pn.ToString();
            Observacion = item.pde_obs;
        }
    }

    private async Task OnGuardarAsync()
    {
        // Validación básica
        if (MaterialId == null || UnidadMedidaId == null || ResponsableId == null || string.IsNullOrWhiteSpace(PesoBruto) || string.IsNullOrWhiteSpace(PesoTara))
        {
            await DialogService.ShowWarning("Complete todos los campos obligatorios.", "Validación");
      
            return;
        }

    }

}
