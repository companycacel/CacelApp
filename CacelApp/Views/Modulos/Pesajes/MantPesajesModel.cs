using CacelApp.Services.Dialog;
using CacelApp.Services.Image;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.DataTable;
using CacelApp.Shared.Controls.ImageViewer;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Services.Configuration;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Enums;
using Infrastructure.Services.Pesajes;
using Infrastructure.Services.Shared;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace CacelApp.Views.Modulos.Pesajes;

/// <summary>
/// ViewModel para el mantenimiento de pesajes (crear/editar)
/// Gestiona el encabezado y los detalles de un pesaje
/// </summary>
public partial class MantPesajesModel : ViewModelBase
{
    private readonly Infrastructure.Services.Pesajes.IPesajesService _pesajesService;
    private readonly IPesajesSearchService _pesajesSearchService;
    private readonly ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    private readonly IConfigurationService _configService;
    private readonly ISerialPortService _serialPortService;
    private readonly ICameraService _cameraService;
    private readonly CacelApp.Services.ImageAudit.IImageAuditService _imageAuditService;
    private Pes? _data;
    #region Propiedades del Encabezado

    [ObservableProperty]
    private int pes_id;

    [ObservableProperty]
    private string? pes_des; 

    [ObservableProperty]
    private string? pes_tipo; // PE, PS, DS

    [ObservableProperty]
    private string? pes_baz_des; 

    [ObservableProperty]
    private string? pes_referencia;

    [ObservableProperty]
    private DateTime pes_fecha = DateTime.Now;

    [ObservableProperty]
    private int pes_status = 3; // 3=REGISTRANDO por defecto

    /// <summary>
    /// Propiedad calculada para mostrar el estado como texto
    /// </summary>
    public string Pes_statusText => Pes_status == 1 ? "PROCESADO" : "REGISTRANDO";

    [ObservableProperty]
    private string? pes_mov_des; // Compra/Tercero

    [ObservableProperty]
    private int? pes_mov_id;

    [ObservableProperty]
    private string? pes_obs;

    [ObservableProperty]
    private string titulo = "NUEVO PESAJE";

    [ObservableProperty]
    private bool esEdicion = false;

    [ObservableProperty]
    private bool esBloqueado = false; // Bloqueado si status=1 (PROCESADO)

    #endregion

    #region Propiedades de Detalles

    [ObservableProperty]
    private ObservableCollection<PesajesDetalleItemDto> detalles = new();

    [ObservableProperty]
    private PesajesDetalleItemDto? detalleSeleccionado;

    [ObservableProperty]
    private string? filtroBusqueda;

    /// <summary>
    /// DataTable para gestionar la tabla de detalles
    /// </summary>
    [ObservableProperty]
    private DataTableViewModel<PesajesDetalleItemDto> detallesTable = new();

    /// <summary>
    /// Columnas configuradas para el DataTable
    /// </summary>
    public ObservableCollection<DataTableColumn> ColumnasDetalles { get; } = new();

    /// <summary>
    /// Acciones del header del DataTable
    /// </summary>
    public ObservableCollection<HeaderActionDef> AccionesHeader { get; } = new();

    [ObservableProperty]
    private PesajesDetalleItemDto itemEdicion = new();

    [ObservableProperty]
    private bool esEdicionDetalle;

    [ObservableProperty]
    private bool esDevolucion;

    /// <summary>
    /// Indica si hay detalles pendientes de guardar (en edición o nuevos)
    /// </summary>
    public bool HasPendingDetails => Detalles.Any(d => d.IsEditing || d.IsNew);

    #endregion

    #region Propiedades de Balanzas

    /// <summary>
    /// Colección dinámica de balanzas configuradas
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo> balanzasInfo = new();

    partial void OnPes_statusChanged(int value)
    {
        OnPropertyChanged(nameof(Pes_statusText));
    }

    #endregion

    #region Opciones de ComboBox

    [ObservableProperty]
    private ObservableCollection<SelectOption> materialOptions = new();
    public ObservableCollection<SelectOption> EstadoOptions { get; } = new();
    public ObservableCollection<SelectOption> BalanzaOptions { get; } = new();

    // Almacena el Ext del material seleccionado (opcional, para uso standalone de FormComboBox)
    [ObservableProperty]
    private object? materialExtData;

    #endregion

    #region Comandos

    public IAsyncRelayCommand GuardarCommand { get; }
    public IAsyncRelayCommand CancelarCommand { get; }
    public IAsyncRelayCommand AgregarDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> EditarDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> EliminarDetalleCommand { get; }
    public IAsyncRelayCommand GuardarDetalleCommand { get; }
    public IAsyncRelayCommand CancelarEdicionDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> VerCapturasCommand { get; }
    public IAsyncRelayCommand BuscarDocumentoCommand { get; }
    public IAsyncRelayCommand BuscarCommand { get; }
    public IAsyncRelayCommand<string> CapturarPesoBalanzaCommand { get; }
    public IRelayCommand ReiniciarSerialCommand { get; }

    #endregion

    public MantPesajesModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        Infrastructure.Services.Pesajes.IPesajesService pesajesService,
        IPesajesSearchService pesajesSearchService,
        ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService,
        IConfigurationService configService,
        ISerialPortService serialPortService,
        ICameraService cameraService,
        CacelApp.Services.ImageAudit.IImageAuditService imageAuditService) : base(dialogService, loadingService)
    {
        _pesajesService = pesajesService ?? throw new ArgumentNullException(nameof(pesajesService));
        _pesajesSearchService = pesajesSearchService ?? throw new ArgumentNullException(nameof(pesajesSearchService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _imageAuditService = imageAuditService ?? throw new ArgumentNullException(nameof(imageAuditService));

        // Inicializar comandos
        GuardarCommand = SafeCommand(GuardarAsync);
        CancelarCommand = SafeCommand(CancelarAsync);
        AgregarDetalleCommand = SafeCommand(AgregarDetalleAsync);
        EditarDetalleCommand = SafeCommand<PesajesDetalleItemDto>(EditarDetalleAsync);
        EliminarDetalleCommand = SafeCommand<PesajesDetalleItemDto>(EliminarDetalleAsync);
        GuardarDetalleCommand = SafeCommand(GuardarDetalleAsync);
        CancelarEdicionDetalleCommand = SafeCommand(CancelarEdicionDetalleAsync);
        VerCapturasCommand = SafeCommand<PesajesDetalleItemDto>(VerCapturasAsync);
        BuscarDocumentoCommand = SafeCommand(BuscarDocumentoAsync);
        CapturarPesoBalanzaCommand = SafeCommand<string>(CapturarPesoBalanzaAsync);
        ReiniciarSerialCommand = new RelayCommand(() =>
        {
            Cleanup();
            IniciarLecturaBalanzas();
        });

        // Configurar opciones de estado
        EstadoOptions.Add(new SelectOption { Value = 0, Label = "ANULADO" });
        EstadoOptions.Add(new SelectOption { Value = 1, Label = "PROCESADO" });
        EstadoOptions.Add(new SelectOption { Value = 2, Label = "PENDIENTE" });
        EstadoOptions.Add(new SelectOption { Value = 3, Label = "REGISTRANDO" });

        // ✅ NO configurar columnas aquí - se configurarán en InicializarAsync según el tipo

        // Configurar acciones del header
        ConfigurarAccionesHeader();

        // Inicializar item de edición
        ResetItemEdicion();

        BuscarCommand = SafeCommand(RefreshDetalleAsync);
    }

    
    /// <summary>
    /// Columnas para pesajes tipo DS (con columna DOC)
    /// </summary>
    private ObservableCollection<DataTableColumn> ColumnasConDoc => new()
    {
        new ColDef<PesajesDetalleItemDto>
        {
            Key = x => x.Pde_mde_des,
            Header = "DOC",
            Width = "150",
            Template = "DetalleDocumentoTemplate"
        },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_bie_des, Header = "MATERIAL", Width = "3*" },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_nbza, Header = "N° B", Width = "2*", Align = "Center" },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_pb, Header = "P. BRUTO", Width = "90", Format = "N2", Type = DataTableColumnType.Number },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_pt, Header = "P. TARA", Width = "90", Format = "N2", Type = DataTableColumnType.Number },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_pn, Header = "P. NETO", Width = "90", Format = "N2", Type = DataTableColumnType.Number },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_obs, Header = "OBSERVACIÓN", Width = "2*" },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Updated, Header = "ACTUALIZACIÓN", Width = "140", Format = "dd/MM/yyyy HH:mm", Type = DataTableColumnType.Date },
        new ColDef<PesajesDetalleItemDto>
        {
            Header = "ACCIONES",
            Width = "160",
            Actions = new List<ActionDef>
            {
                new ActionDef { Icon = PackIconKind.Pencil, Command = EditarDetalleCommand, Tooltip = "Editar", IconSize = 28, Disabled = x => !((PesajesDetalleItemDto)x).CanEdit },
                new ActionDef { Icon = PackIconKind.Visibility, Command = VerCapturasCommand, Tooltip = "Ver Capturas", IconSize = 28, Color = "#ffa726", VisibilityProperty = nameof(PesajesDetalleItemDto.HasImages) },
                new ActionDef { Icon = PackIconKind.Delete, Command = EliminarDetalleCommand, Tooltip = "Eliminar", IconSize = 28, Color = "#EF4444", Disabled = x => !((PesajesDetalleItemDto)x).CanDelete }
            }
        }
    };

    /// <summary>
    /// Columnas para pesajes tipo PE/PS (sin columna DOC)
    /// </summary>
    private ObservableCollection<DataTableColumn> ColumnasSinDoc => new()
    {
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_bie_des, Header = "MATERIAL", Width = "3*" },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_nbza, Header = "N° B", Width = "2*", Align = "Center" },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_pb, Header = "P. BRUTO", Width = "90", Format = "N2", Type = DataTableColumnType.Number },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_pt, Header = "P. TARA", Width = "90", Format = "N2", Type = DataTableColumnType.Number },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_pn, Header = "P. NETO", Width = "90", Format = "N2", Type = DataTableColumnType.Number },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Pde_obs, Header = "OBSERVACIÓN", Width = "2*" },
        new ColDef<PesajesDetalleItemDto> { Key = x => x.Updated, Header = "ACTUALIZACIÓN", Width = "140", Format = "dd/MM/yyyy HH:mm", Type = DataTableColumnType.Date },
        new ColDef<PesajesDetalleItemDto>
        {
            Header = "ACCIONES",
            Width = "160",
            Actions = new List<ActionDef>
            {
                new ActionDef { Icon = PackIconKind.Pencil, Command = EditarDetalleCommand, Tooltip = "Editar", IconSize = 28, Disabled = x => !((PesajesDetalleItemDto)x).CanEdit },
                new ActionDef { Icon = PackIconKind.Visibility, Command = VerCapturasCommand, Tooltip = "Ver Capturas", IconSize = 28, Color = "#ffa726", VisibilityProperty = nameof(PesajesDetalleItemDto.HasImages) },
                new ActionDef { Icon = PackIconKind.Delete, Command = EliminarDetalleCommand, Tooltip = "Eliminar", IconSize = 28, Color = "#EF4444", Disabled = x => !((PesajesDetalleItemDto)x).CanDelete }
            }
        }
    };

    /// <summary>
    /// Configura las columnas según el tipo de pesaje
    /// </summary>
    private void ConfigurarColumnasDetalles(bool esDevolucion)
    {
        ColumnasDetalles.Clear();
        var columnas = esDevolucion ? ColumnasConDoc : ColumnasSinDoc;
        foreach (var columna in columnas)
        {
            ColumnasDetalles.Add(columna);
        }
    }

    /// <summary>
    /// Inicializa el formulario con un pesaje existente o nuevo
    /// </summary>
    public async Task InicializarAsync(Pes? pesaje = null, string? tipo = null)
    {
        try
        {
            _ = CargarMaterialesAsync(pesaje?.pes_mov_id);
            CargarBalanzasDisponibles();
            IniciarLecturaBalanzas();

            if (pesaje != null)
            {
                EsEdicion = true;
                EsDevolucion = pesaje.pes_tipo == "DS";
                await CargarPesajeAsync(pesaje);
                ConfigurarColumnasDetalles(pesaje.pes_tipo == "DS");
            }
            else if (!string.IsNullOrEmpty(tipo))
            {
                Pes_tipo = tipo;
                EsDevolucion = tipo == "DS";
                Titulo = $"NUEVO PESAJE {GetTipoDescripcion(tipo)}";
                ConfigurarColumnasDetalles(tipo == "DS");
            }
            _data = pesaje;
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al inicializar");
        }
        finally
        {
            LoadingService.StopLoading();
        }     
    }

    private string GetTipoDescripcion(string tipo)
    {
        return tipo switch
        {
            "PE" => "ENTRADA",
            "PS" => "SALIDA",
            "DS" => "DEVOLUCIÓN",
            _ => tipo
        };
    }

    private async Task CargarMaterialesAsync(int? movId)
    {
        try
        {
            var materiales = await _selectOptionService.GetSelectOptionsAsync(SelectOptionType.Material, null, new { _bie_mov_id = movId });

            MaterialOptions.Clear();
            foreach (var material in materiales)
            {
                // Asegurar que Value sea int para que coincida con Pde_bie_id
                var valorInt = 0;
                if (material.Value != null)
                {
                    if (material.Value is int intVal)
                        valorInt = intVal;
                    else if (int.TryParse(material.Value.ToString(), out int parsed))
                        valorInt = parsed;
                }

                MaterialOptions.Add(new SelectOption
                {
                    Value = valorInt,  
                    Label = material.Label,
                    Ext = material.Ext  
                });
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar materiales: {ex.Message}", "Error");
        }
    }

    private async void CargarBalanzasDisponibles()
    {
        BalanzaOptions.Clear();
        var sede = await _configService.GetSedeActivaAsync();

        if (sede != null)
        {
            foreach (var balanza in sede.Balanzas)
            {
                BalanzaOptions.Add(new SelectOption { Label = balanza.Nombre, Value = balanza.Nombre });
            }
            BalanzaOptions.Add(new SelectOption { Label = "B0-O", Value = "B0-O" });
        }
    }

    private async Task CargarPesajeAsync(Pes pesaje)
    {
        Pes_id = pesaje.pes_id;
        Pes_des = pesaje.pes_des;
        Pes_tipo = pesaje.pes_tipo;
        Pes_baz_des = pesaje.pes_baz_des;
        Pes_referencia = pesaje.pes_referencia;
        Pes_fecha = pesaje.pes_fecha;
        Pes_status = pesaje.pes_status;
        Pes_mov_des = pesaje.pes_mov_des;
        Pes_mov_id = pesaje.pes_mov_id;
        Pes_obs = pesaje.pes_obs;

        Titulo = $"{GetTipoDescripcion(pesaje.pes_tipo)} N° {pesaje.pes_des}";
        EsBloqueado = pesaje.pes_status == 1; 
        if (pesaje.pdes != null && pesaje.pdes.Any())
        {
            var detallesDto = pesaje.pdes
                .Select(MapearDetalleADto)
                .ToList();
            Detalles = new ObservableCollection<PesajesDetalleItemDto>(detallesDto);
        }
    }

    private PesajesDetalleItemDto MapearDetalleADto(Pde detalle)
    {
        return new PesajesDetalleItemDto
        {
            Pde_id = detalle.pde_id,
            Pde_pes_id = detalle.pde_pes_id,
            Pde_mde_id = detalle.pde_mde_id,
            Pde_mde_des = detalle.pde_mde_des,
            Pde_bie_id = detalle.pde_bie_id,
            // Ahora Value es int, comparación directa
            Pde_bie_des = !string.IsNullOrEmpty(detalle.pde_bie_des) ? detalle.pde_bie_des : MaterialOptions.FirstOrDefault(m => (int)(m.Value ?? 0) == detalle.pde_bie_id)?.Label,
            Pde_nbza = detalle.pde_nbza,
            Pde_pb = detalle.pde_pb.ToString("0.00"),
            Pde_pt = detalle.pde_pt.ToString("0.00"),
            Pde_pn = detalle.pde_pn.ToString("0.00"),
            Pde_obs = detalle.pde_obs,
            Pde_gus_des = detalle.pde_gus_des,
            Created = detalle.created,
            Updated = detalle.updated,
            Pde_path = detalle.pde_path,
            Pde_media = detalle.pde_media,
            Pde_t6m_id = detalle.pde_t6m_id,
            Pde_bie_cod = detalle.pde_bie_cod,
            CanEdit = !EsBloqueado,
            CanDelete = !EsBloqueado,
            IsEditing = false,
            IsNew = false,
            // ✅ Inyectar referencias para extraer Ext automáticamente
            MaterialOptionsReference = MaterialOptions,
            GetValueFromExtFunc = GetValueFromObject<int?>
        };
    }

    #region Métodos de Comandos

    private async Task GuardarAsync()
    {
        try
        {
            if (!Detalles.Any())
            {
                await DialogService.ShowWarning("Validación", "Debe agregar al menos un detalle");
                return;
            }
            if (HasUnsavedFormData())
            {
                var guardarDetalle = await DialogService.ShowConfirm("Tiene un detalle con datos sin guardar. ¿Desea guardarlo antes de continuar?", "Datos pendientes");

                if (guardarDetalle)
                {
                    await GuardarDetalleAsync();
                    if (HasPendingDetails)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            if (Detalles.Any(d => d.IsEditing))
            {
                await DialogService.ShowWarning("Primero guarde o cancele los detalles en edición", "Validación");
                return;
            }

            _data.pes_id = Pes_id;
            _data.pes_des = Pes_des;
            _data.pes_tipo = Pes_tipo;
            _data.pes_baz_des = Pes_baz_des;
            _data.pes_referencia = Pes_referencia;
            _data.pes_fecha = Pes_fecha;
            _data.pes_status = Pes_status;
            _data.pes_mov_id = Pes_mov_id;
            _data.pes_obs = Pes_obs;
            _data.action = EsEdicion ? ActionType.Update : ActionType.Create;
            _data.pdes = Detalles.Select(d => new Pde
            {
                pde_id = d.Pde_id,
                pde_pes_id = d.Pde_pes_id,
                pde_mde_id = d.Pde_mde_id,
                pde_bie_id = d.Pde_bie_id,
                pde_nbza = d.Pde_nbza,
                pde_pb = float.TryParse(d.Pde_pb, out float pb) ? pb : 0,
                pde_pt = float.TryParse(d.Pde_pt, out float pt) ? pt : 0,
                pde_pn = float.TryParse(d.Pde_pn, out float pn) ? pn : 0,
                pde_obs = d.Pde_obs,
                pde_tipo = new[] { "PE", "DS" }.Contains(Pes_tipo) ? 2 : 1,
                pde_t6m_id = d.Pde_t6m_id
            }).ToList();


            var response = await _pesajesService.SavePesajeAsync(_data);

            if (response.status != 1)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "Error al guardar", "Error");
                return;
            }

            await DialogService.ShowSuccess(response.Meta?.msg ?? "Guardado exitosamente", "Éxito");

            // Cerrar ventana
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al guardar");
        }
    }

    private async Task CancelarAsync()
    {
        if (Detalles.Any(d => d.IsEditing || d.IsNew))
        {
            var confirmar = await DialogService.ShowConfirm(
                "Tiene cambios sin guardar. ¿Desea salir sin guardar?",
                "Confirmar");

            if (!confirmar) return;
        }

        RequestClose?.Invoke(false);
    }

    private async Task AgregarDetalleAsync()
    {
        // Validar que no haya otro detalle en edición
        if (Detalles.Any(d => d.IsEditing))
        {
            await DialogService.ShowWarning("Primero guarde o cancele el detalle en edición", "Validación");
            return;
        }

        var nuevoDetalle = new PesajesDetalleItemDto
        {
            Pde_pes_id = Pes_id,
            IsNew = true,
            IsEditing = true,
            CanEdit = true,
            CanDelete = true,
            Created = DateTime.Now,
            Pde_pb = null,
            Pde_pt = null,
            Pde_pn = null,
            // ✅ Inyectar referencias para extraer Ext automáticamente
            MaterialOptionsReference = MaterialOptions,
            GetValueFromExtFunc = GetValueFromObject<int?>
        };

        Detalles.Insert(0, nuevoDetalle);
        DetalleSeleccionado = nuevoDetalle;
        ActualizarDetallesTable();
    }

    private async Task EditarDetalleAsync(PesajesDetalleItemDto? detalle)
    {
        if (detalle == null || !detalle.CanEdit) return;

        // Clonar item a ItemEdicion para el panel
        ItemEdicion = new PesajesDetalleItemDto
        {
            Pde_id = detalle.Pde_id,
            Pde_pes_id = detalle.Pde_pes_id,
            Pde_mde_id = detalle.Pde_mde_id,
            Pde_mde_des = detalle.Pde_mde_des,
            Pde_bie_id = detalle.Pde_bie_id,
            Pde_bie_des = detalle.Pde_bie_des,
            Pde_nbza = detalle.Pde_nbza,
            Pde_pb = detalle.Pde_pb,
            Pde_pt = detalle.Pde_pt,
            Pde_pn = detalle.Pde_pn,
            Pde_obs = detalle.Pde_obs,
            Pde_path = detalle.Pde_path,
            Pde_media = detalle.Pde_media,
            Pde_t6m_id = detalle.Pde_t6m_id,
            Pde_bie_cod = detalle.Pde_bie_cod,

            IsNew = false,
            IsEditing = true,
            CanEdit = true,
            CanDelete = true,

            MaterialOptionsReference = MaterialOptions,
            GetValueFromExtFunc = GetValueFromObject<int?>
        };

        EsEdicionDetalle = true;

        await Task.CompletedTask;
    }
    private async Task RefreshDetalleAsync()
    {
        if (_data != null && _data.pes_id > 0)
        {
            try
            {
                var responseDetail = await _pesajesSearchService.GetPesajesDetalleAsync(_data.pes_id);
                if (responseDetail?.Data != null)
                {
                    Detalles.Clear();
                    foreach (var det in responseDetail.Data)
                    {
                        Detalles.Add(MapearDetalleADto(det));
                    }
                    ActualizarDetallesTable();
                }
            }
            catch (Exception refreshEx)
            {
            }
        }

    }
    private async Task EliminarDetalleAsync(PesajesDetalleItemDto? detalle)
    {
        if (detalle == null) return;

        // Si es nuevo, solo removerlo de la lista
        if (detalle.IsNew)
        {
            Detalles.Remove(detalle);
            return;
        }

        if (!detalle.CanDelete) return;

        var confirmar = await DialogService.ShowConfirm(
            "¿Está seguro de eliminar este detalle?",
            "Confirmar eliminación");

        if (!confirmar) return;

        try
        {
            var pde = new Pde
            {
                pde_id = detalle.Pde_id,
                pde_pes_id = detalle.Pde_pes_id,
                action = ActionType.Delete
            };

            var response = await _pesajesService.SavePesajeDetalleAsync(pde);

            if (response.status != 1)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "Error al eliminar", "Error");
                return;
            }

            Detalles.Remove(detalle);
            await RefreshDetalleAsync();

            await DialogService.ShowSuccess("Detalle eliminado correctamente", "Éxito");

        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al eliminar");
        }
    }

    private async Task GuardarDetalleAsync()
    {
        var detalle = ItemEdicion;
        if (detalle == null) return;

        // Validaciones
        if (detalle.Pde_bie_id <= 0)
        {
            await DialogService.ShowWarning("Seleccione un material", "Validación");
            return;
        }

        if (string.IsNullOrEmpty(detalle.Pde_nbza))
        {
            await DialogService.ShowWarning("Seleccione una balanza", "Validación");
            return;
        }

        if (!decimal.TryParse(detalle.Pde_pb, out decimal pbVal) || pbVal <= 0)
        {
            await DialogService.ShowWarning("Ingrese el peso bruto", "Validación");
            return;
        }

        decimal.TryParse(detalle.Pde_pt, out decimal ptVal);
        if (ptVal > pbVal)
        {
            await DialogService.ShowWarning("La tara no puede ser superior al peso bruto", "Validación");
            return;
        }
        if (detalle.Pde_t6m_id == null || detalle.Pde_t6m_id <= 0)
        {
            // Intentar recuperarlo del MaterialExtData como fallback
            var t6mId = GetValueFromObject<int?>(MaterialExtData, "bie_t6m_id");

            if (t6mId.HasValue && t6mId.Value > 0)
            {
                detalle.Pde_t6m_id = t6mId.Value;
            }
        }
        var pde = new Pde
        {
            pde_id = detalle.Pde_id,
            pde_pes_id = _data.pes_id,
            pde_mde_id = detalle.Pde_mde_id??0,
            pde_bie_id = detalle.Pde_bie_id,
            pde_nbza = detalle.Pde_nbza,
            pde_pb = float.TryParse(detalle.Pde_pb, out float pb) ? pb : 0,
            pde_pt = float.TryParse(detalle.Pde_pt, out float pt) ? pt : 0,
            pde_pn = float.TryParse(detalle.Pde_pn, out float pn) ? pn : 0,
            pde_obs = detalle.Pde_obs,
            pde_tipo = new[] { "PE", "DS" }.Contains(Pes_tipo) ? 2 : 1,
            pde_t6m_id = detalle.Pde_t6m_id,
            action = EsEdicionDetalle ? ActionType.Update : ActionType.Create
        };

        // Agregar fotos capturadas si existen
        if (detalle.FotosCapturas != null && detalle.FotosCapturas.Any())
        {
            pde.files = detalle.FotosCapturas.Select(f =>
                new Infrastructure.Services.Shared.SimpleFormFile(f.contenido, "files", f.nombre) as Microsoft.AspNetCore.Http.IFormFile
            ).ToList();
        }
        var response = await _pesajesService.SavePesajeDetalleAsync(pde);

        if (response.status != 1)
        {
            await DialogService.ShowError(response.Meta?.msg ?? "Error al guardar", "Error");
            return;
        }

        // Actualizar el detalle con los datos guardados
        detalle.Pde_id = response.Data.pde_id;
        detalle.Pde_path = response.Data.pde_path;
        detalle.Pde_media = response.Data.pde_media;
        detalle.Pde_gus_des = response.Data.pde_gus_des;
        detalle.Updated = response.Data.updated;
        detalle.IsNew = false;
        detalle.IsEditing = false;

        // Actualizar descripción del material - Value ahora es int
        detalle.Pde_bie_des = MaterialOptions.FirstOrDefault(m => (int)(m.Value ?? 0) == detalle.Pde_bie_id)?.Label;

        if (EsEdicionDetalle)
        {
            var original = Detalles.FirstOrDefault(d => d.Pde_id == detalle.Pde_id);
            if (original != null)
            {
                var index = Detalles.IndexOf(original);
                Detalles[index] = detalle;
            }
        }
        else
        {
            Detalles.Insert(0, detalle);
            
            if (detalle.FotosCapturas != null && detalle.FotosCapturas.Any())
            {
                var imagenes = detalle.FotosCapturas.Select(f => new System.IO.MemoryStream(f.contenido)).ToList();
                await _imageAuditService.GuardarImagenesLocalmenteAsync(
                    imagenes,
                    response.Data.pde_path,
                    response.Data.pde_media);
            }
        }

        ResetItemEdicion();
        ActualizarDetallesTable();

        await DialogService.ShowSuccess($"{response.Meta.msg}", "Éxito");

    }

    private async Task CancelarEdicionDetalleAsync()
    {
        ResetItemEdicion();
        await Task.CompletedTask;
    }

    private async Task VerCapturasAsync(PesajesDetalleItemDto? detalle)
    {
        if (detalle == null || !detalle.HasImages)
        {
            await DialogService.ShowInfo("No hay imágenes capturadas", "Información");
            return;
        }


        // Cargar imágenes desde el servidor
        var imagenes = await _imageLoaderService.CargarImagenesAsync(
            detalle.Pde_path ?? string.Empty,
            detalle.Pde_media ?? string.Empty);

        if (imagenes == null || imagenes.Count == 0)
        {
            await DialogService.ShowWarning(
                "No se pudieron cargar las imágenes desde el servidor",
                "Advertencia");
            return;
        }

        // Crear ViewModel con el componente existente
        var viewModel = new ImageViewerViewModel(
            imagenes,
            null, // Sin imágenes de destare
            $"Detalle #{detalle.Pde_id} - {detalle.Pde_bie_des}");

        // Abrir ventana usando el componente existente
        var viewer = new ImageViewerWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        viewer.ShowDialog();

    }
    private async Task CapturarPesoBalanzaAsync(string nombreBalanza)
    {
        var balanza = BalanzasInfo.FirstOrDefault(b => b.Nombre == nombreBalanza);
        if (balanza == null) return;

        await CapturarPesoAsync(balanza.PesoActual, nombreBalanza);
    }

    private async Task CapturarPesoAsync(decimal? peso, string nombreBalanza)
    {
        if (string.IsNullOrEmpty(peso.ToString()))
        {
            await DialogService.ShowWarning("No se ha capturado el peso de la balanza", "Advertencia");
            return;
        }

        // Usar ItemEdicion directamente para el panel de entrada
        var detalleEditable = ItemEdicion;

        if (detalleEditable == null) return;

        // Asignar valores (convertir a string)
        detalleEditable.Pde_pb = (peso ?? 0).ToString("0.00");
        detalleEditable.Pde_pt = "0.00";
        detalleEditable.Pde_nbza = nombreBalanza;

        // Capturar fotos desde cámaras
        await CapturarFotosAsync(detalleEditable, nombreBalanza);

    }

    private async Task CapturarFotosAsync(PesajesDetalleItemDto detalle, string nombreBalanza)
    {
        try
        {
            // No capturar fotos si es balanza B0-O (balanza sin cámaras)
            if (nombreBalanza == "B0-O")
            {
                return;
            }

            if (_cameraService == null) return;

            var sede = await _configService.GetSedeActivaAsync();
            var balanza = sede?.Balanzas.FirstOrDefault(b => b.Nombre == nombreBalanza);

            if (balanza == null || !balanza.CanalesCamaras.Any()) return;

            // Limpiar memoria de imágenes anteriores antes de capturar nuevas
            if (detalle.FotosCapturas != null)
            {
                detalle.FotosCapturas.Clear();
            }
            detalle.FotosCapturas = new List<(string nombre, byte[] contenido)>();

            foreach (var canal in balanza.CanalesCamaras)
            {
                var stream = await _cameraService.CapturarImagenAsync(canal);
                if (stream != null)
                {
                    var bytes = stream.ToArray();
                    var nombre = $"{canal}.jpg";
                    detalle.FotosCapturas.Add((nombre, bytes));
                    stream.Dispose();
                }
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al capturar fotos: {ex.Message}");
        }
    }

    private async Task BuscarDocumentoAsync()
    {
        try
        {
            // Usar ItemEdicion
            var detalleEnEdicion = ItemEdicion;
            if (detalleEnEdicion == null) return;

            // Crear el modal usando el constructor con inyección de dependencias
            var documentosModel = new DocumentosModel(DialogService, LoadingService, _pesajesSearchService);
            var modal = new Documentos(documentosModel);
            var resultado = modal.ShowDialog();

            if (resultado == true && documentosModel.DocumentoSeleccionado != null)
            {
                var docSeleccionado = documentosModel.DocumentoSeleccionado;

                // Actualizar el detalle con el documento seleccionado
                detalleEnEdicion.Pde_mde_id = docSeleccionado.mde_id;
                detalleEnEdicion.Pde_mde_des = docSeleccionado.mde_mov_des;

                // No es necesario refrescar la tabla aquí
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al buscar documento: {ex.Message}", "Error");
        }
    }

    #endregion

    #region Métodos Auxiliares del DataTable

    /// <summary>
    /// Actualiza el DataTable con los datos actuales de Detalles
    /// </summary>
    private void ActualizarDetallesTable()
    {
        DetallesTable.SetData(Detalles);

        // No configurar totales para esta vista específica
        // DetallesTable.ConfigureTotals(new[] { "Pde_pn" });

        // Configurar filtro personalizado
        DetallesTable.CustomFilter = (detalle, searchTerm) =>
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return true;

            var term = searchTerm.ToLower();
            return (detalle.Pde_bie_des?.ToLower().Contains(term) ?? false) ||
                   (detalle.Pde_nbza?.ToLower().Contains(term) ?? false) ||
                   (detalle.Pde_obs?.ToLower().Contains(term) ?? false);
        };
    }

    /// <summary>
    /// Se llama cuando se modifica la colección Detalles
    /// </summary>
    partial void OnDetallesChanged(ObservableCollection<PesajesDetalleItemDto> value)
    {
        ActualizarDetallesTable();
    }

    /// <summary>
    /// Configura las acciones del header del DataTable
    /// </summary>
    private void ConfigurarAccionesHeader()
    {
        AccionesHeader.Clear();
        // Las acciones ahora están en el panel de entrada
    }

    private void ResetItemEdicion()
    {
        ItemEdicion = new PesajesDetalleItemDto
        {
            IsNew = true,
            IsEditing = true, // Para habilitar controles en el panel
            MaterialOptionsReference = MaterialOptions,
            GetValueFromExtFunc = GetValueFromObject<int?>
        };
        EsEdicionDetalle = false;
    }

    #endregion
    public Action<bool>? RequestClose { get; set; }

    private Dictionary<string, string> _balanzaPuertoMap = new();

    private async void IniciarLecturaBalanzas()
    {
        var sede = await _configService.GetSedeActivaAsync();
        if (sede != null && sede.Balanzas.Any())
        {
            // Crear dinámicamente la colección de balanzas para la UI
            BalanzasInfo.Clear();
            _balanzaPuertoMap.Clear();

            var colores = new[] { "#4F46E5", "#10B981", "#F59E0B", "#EF4444" };
            int colorIndex = 0;

            foreach (var balanza in sede.Balanzas)
            {
                if (!string.IsNullOrEmpty(balanza.Puerto))
                {
                    _balanzaPuertoMap[balanza.Puerto] = balanza.Nombre;
                }

                // Capturar el nombre de la balanza en una variable local para evitar closure issues
                var nombreBalanza = balanza.Nombre;

                var balanzaInfo = new CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo
                {
                    Nombre = balanza.Nombre,
                    Puerto = balanza.Puerto,
                    ColorBorde = colores[colorIndex % colores.Length],
                    Conectada = balanza.Conectada,
                    MostrarBotonCaptura = true,
                    CapturarCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                            await CapturarPesoBalanzaAsync(nombreBalanza)))
                };

                BalanzasInfo.Add(balanzaInfo);
                colorIndex++;
            }

            // ✅ Remover handlers existentes ANTES de agregar nuevos para evitar acumulación
            _serialPortService.OnPesosLeidos -= OnPesosLeidos;
            _serialPortService.OnEstabilidadCambiada -= OnEstabilidadCambiada;
            
            // Ahora agregar los handlers
            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            _serialPortService.OnEstabilidadCambiada += OnEstabilidadCambiada;

            var ultimasLecturas = _serialPortService.ObtenerUltimasLecturas();
            if (ultimasLecturas.Any())
            {
                OnPesosLeidos(ultimasLecturas);
            }

            // Inicializar estado de estabilidad
            var estabilidadActual = _serialPortService.ObtenerEstabilidadActual();
            if (estabilidadActual.Any())
            {
                OnEstabilidadCambiada(estabilidadActual);
            }

            _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
        }
    }

    private void OnEstabilidadCambiada(Dictionary<string, bool> estabilidades)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var estabilidad in estabilidades)
            {
                var balanzaInfo = BalanzasInfo.FirstOrDefault(b => b.Puerto == estabilidad.Key);
                if (balanzaInfo != null)
                {
                    balanzaInfo.EsEstable = estabilidad.Value;
                }
            }
        });
    }

    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        // Actualizar propiedades en el hilo de la UI
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var lectura in lecturas)
            {
                // Buscar la balanza por puerto y actualizar su peso
                var balanzaInfo = BalanzasInfo.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanzaInfo != null && decimal.TryParse(lectura.Value, out decimal peso))
                {
                    balanzaInfo.PesoActual = peso;
                    balanzaInfo.Conectada = true;
                }
            }
        });
    }

    /// <summary>
    /// Verifica si hay datos sin guardar en el formulario de edición de detalle
    /// </summary>
    private bool HasUnsavedFormData()
    {
        if (ItemEdicion == null) return false;

        // Verificar si hay algún campo con datos
        return ItemEdicion.Pde_bie_id > 0 ||
               !string.IsNullOrWhiteSpace(ItemEdicion.Pde_nbza) ||
               !string.IsNullOrWhiteSpace(ItemEdicion.Pde_pb) ||
               !string.IsNullOrWhiteSpace(ItemEdicion.Pde_pt) ||
               !string.IsNullOrWhiteSpace(ItemEdicion.Pde_obs);
    }

    public void Cleanup()
    {
        _serialPortService.OnPesosLeidos -= OnPesosLeidos;
        _serialPortService.OnEstabilidadCambiada -= OnEstabilidadCambiada;
        _serialPortService.DetenerLectura();
        
        // ✅ Limpiar MaterialOptions para liberar memoria
        MaterialOptions.Clear();
    }
}
