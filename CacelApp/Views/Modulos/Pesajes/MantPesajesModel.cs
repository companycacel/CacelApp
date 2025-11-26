using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Services.Image;
using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CacelApp.Shared.Controls.ImageViewer;
using CacelApp.Shared.Controls.DataTable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Pesajes;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Enums;
using Core.Services.Configuration;
using Infrastructure.Services.Shared;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CacelApp.Views.Modulos.Pesajes;

/// <summary>
/// ViewModel para el mantenimiento de pesajes (crear/editar)
/// Gestiona el encabezado y los detalles de un pesaje
/// </summary>
public partial class MantPesajesModel : ViewModelBase
{
    private readonly IPesajesService _pesajesService;
    private readonly ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    private readonly IConfigurationService _configService;
    private readonly ISerialPortService _serialPortService;

    #region Propiedades del Encabezado

    [ObservableProperty]
    private int pes_id;

    [ObservableProperty]
    private string? pes_des; // Código del pesaje

    [ObservableProperty]
    private string? pes_tipo; // PE, PS, DS

    [ObservableProperty]
    private string? pes_baz_des; // Ticket de balanza

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

    #endregion

    #region Propiedades de Balanzas

    [ObservableProperty]
    private string? pesoB1; // Peso actual de balanza 1

    [ObservableProperty]
    private string? pesoB2; // Peso actual de balanza 2

    [ObservableProperty]
    private string nombreB1 = "B1-A";

    [ObservableProperty]
    private string nombreB2 = "B2-A";

    partial void OnPes_statusChanged(int value)
    {
        OnPropertyChanged(nameof(Pes_statusText));
    }

    [ObservableProperty]
    private bool estadoCamaraB1; // true=verde, false=rojo

    [ObservableProperty]
    private bool estadoCamaraB2;

    #endregion

    #region Opciones de ComboBox

    public ObservableCollection<SelectOption> EstadoOptions { get; } = new();
    public ObservableCollection<SelectOption> MaterialOptions { get; } = new();
    public ObservableCollection<string> BalanzaOptions { get; } = new();

    #endregion

    #region Comandos

    public IAsyncRelayCommand GuardarCommand { get; }
    public IAsyncRelayCommand CancelarCommand { get; }
    public IAsyncRelayCommand AgregarDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> EditarDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> EliminarDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> GuardarDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> CancelarEdicionDetalleCommand { get; }
    public IAsyncRelayCommand<PesajesDetalleItemDto> VerCapturasCommand { get; }
    public IAsyncRelayCommand CapturarB1Command { get; }
    public IAsyncRelayCommand CapturarB2Command { get; }
    public IAsyncRelayCommand<string> BuscarDocumentoCommand { get; }

    #endregion

    public MantPesajesModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IPesajesService pesajesService,
        ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService,
        IConfigurationService configService,
        ISerialPortService serialPortService) : base(dialogService, loadingService)
    {
        _pesajesService = pesajesService ?? throw new ArgumentNullException(nameof(pesajesService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));

        // Inicializar comandos
        GuardarCommand = new AsyncRelayCommand(GuardarAsync);
        CancelarCommand = new AsyncRelayCommand(CancelarAsync);
        AgregarDetalleCommand = new AsyncRelayCommand(AgregarDetalleAsync);
        EditarDetalleCommand = new AsyncRelayCommand<PesajesDetalleItemDto>(EditarDetalleAsync);
        EliminarDetalleCommand = new AsyncRelayCommand<PesajesDetalleItemDto>(EliminarDetalleAsync);
        GuardarDetalleCommand = new AsyncRelayCommand<PesajesDetalleItemDto>(GuardarDetalleAsync);
        CancelarEdicionDetalleCommand = new AsyncRelayCommand<PesajesDetalleItemDto>(CancelarEdicionDetalleAsync);
        VerCapturasCommand = new AsyncRelayCommand<PesajesDetalleItemDto>(VerCapturasAsync);
        CapturarB1Command = new AsyncRelayCommand(CapturarB1Async);
        CapturarB2Command = new AsyncRelayCommand(CapturarB2Async);
        BuscarDocumentoCommand = new AsyncRelayCommand<string>(BuscarDocumentoAsync);

        // Configurar opciones de estado
        EstadoOptions.Add(new SelectOption { Value = 0, Label = "ANULADO" });
        EstadoOptions.Add(new SelectOption { Value = 1, Label = "PROCESADO" });
        EstadoOptions.Add(new SelectOption { Value = 2, Label = "PENDIENTE" });
        EstadoOptions.Add(new SelectOption { Value = 3, Label = "REGISTRANDO" });

        // Configurar columnas del DataTable
        ConfigurarColumnasDetalles();
    }

    /// <summary>
    /// Configura las columnas del DataTable de detalles con tipado fuerte
    /// </summary>
    private void ConfigurarColumnasDetalles()
    {
        ColumnasDetalles.Clear();

        // Columna: Material (ComboBox editable)
        var materialCol = new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Key(x => x.Pde_bie_id)
            .Header("MATERIAL")
            .Width("2*")
            .AsType(DataTableColumnType.ComboBox);
        materialCol._column.ComboBoxItemsSource = MaterialOptions;
        materialCol._column.ComboBoxDisplayMemberPath = "Label";
        materialCol._column.ComboBoxSelectedValuePath = "Value";
        materialCol._column.IsReadOnly = false;
        ColumnasDetalles.Add(materialCol);

        // Columna: N° Balanza (ComboBox editable - lista simple de strings)
        var balanzaCol = new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Key(x => x.Pde_nbza)
            .Header("N° B")
            .Width("100")
            .Align("Center")
            .AsType(DataTableColumnType.ComboBox);
        balanzaCol._column.ComboBoxItemsSource = BalanzaOptions;
        balanzaCol._column.IsReadOnly = false;
        ColumnasDetalles.Add(balanzaCol);

        // Columna: Peso Bruto (Editable)
        var pbCol = new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Key(x => x.Pde_pb)
            .Header("P. BRUTO (KG)")
            .Width("90")
            .AsNumber("N2");
        pbCol._column.ColumnType = DataTableColumnType.EditableNumber;
        pbCol._column.IsReadOnly = false;
        ColumnasDetalles.Add(pbCol);

        // Columna: Peso Tara (Editable)
        var ptCol = new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Key(x => x.Pde_pt)
            .Header("P. TARA (KG)")
            .Width("90")
            .AsNumber("N2");
        ptCol._column.ColumnType = DataTableColumnType.EditableNumber;
        ptCol._column.IsReadOnly = false;
        ColumnasDetalles.Add(ptCol);

        // Columna: Peso Neto (Solo lectura, calculado)
        ColumnasDetalles.Add(new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Key(x => x.Pde_pn)
            .Header("P. NETO (KG)")
            .Width("90")
            .AsNumber("N2")
            .Total(true));

        // Columna: Observación (Editable)
        var obsCol = new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Key(x => x.Pde_obs)
            .Header("OBSERVACIÓN")
            .Width("2*")
            .AsType(DataTableColumnType.EditableText);
        obsCol._column.IsReadOnly = false;
        ColumnasDetalles.Add(obsCol);

        // Columna: Ver Capturas (Ícono de cámara)
        ColumnasDetalles.Add(new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Header("")
            .Width("50")
            .AsTemplate("DetalleCapturasTemplate"));

        // Columna: Acciones (Edit/Delete cuando NO está editando, Save/Cancel cuando SÍ está editando)
        ColumnasDetalles.Add(new DataTableColumnBuilder<PesajesDetalleItemDto>()
            .Header("ACCIONES")
            .Width("120")
            .AsTemplate("DetalleAccionesTemplate"));
    }

    /// <summary>
    /// Inicializa el formulario con un pesaje existente o nuevo
    /// </summary>
    public async Task InicializarAsync(Pes? pesaje = null, string? tipo = null)
    {
        try
        {
            LoadingService.StartLoading();

            // Cargar opciones de materiales
            await CargarMaterialesAsync(pesaje?.pes_mov_id);

            // Cargar opciones de balanzas
            CargarBalanzasDisponibles();

            // Iniciar lectura de balanzas
            IniciarLecturaBalanzas();

            if (pesaje != null)
            {
                // Modo edición
                EsEdicion = true;
                await CargarPesajeAsync(pesaje);
            }
            else if (!string.IsNullOrEmpty(tipo))
            {
                // Modo creación
                Pes_tipo = tipo;
                Titulo = $"NUEVO PESAJE {GetTipoDescripcion(tipo)}";
            }
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
            var materiales = await _selectOptionService.GetSelectOptionsAsync(
                SelectOptionType.Material,
                movId);

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
                    Value = valorInt,  // Ahora es int, no object
                    Label = material.Label 
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
                BalanzaOptions.Add(balanza.Nombre);
            }
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
        EsBloqueado = pesaje.pes_status == 1; // Bloqueado si está PROCESADO

        // Cargar detalles
        if (pesaje.pdes != null && pesaje.pdes.Any())
        {
            foreach (var detalle in pesaje.pdes)
            {
                Detalles.Add(MapearDetalleADto(detalle));
            }
        }

        // Actualizar DataTable
        ActualizarDetallesTable();
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
            Pde_bie_des = MaterialOptions.FirstOrDefault(m => (int)(m.Value ?? 0) == detalle.pde_bie_id)?.Label,
            Pde_nbza = detalle.pde_nbza,
            Pde_pb = (decimal)detalle.pde_pb,
            Pde_pt = (decimal)detalle.pde_pt,
            Pde_pn = (decimal)detalle.pde_pn,
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
            IsNew = false
        };
    }

    #region Métodos de Comandos

    private async Task GuardarAsync()
    {
        try
        {
            // Validar que tenga al menos un detalle
            if (!Detalles.Any())
            {
                await DialogService.ShowWarning("Debe agregar al menos un detalle", "Validación");
                return;
            }

            // Validar que no haya detalles en edición
            if (Detalles.Any(d => d.IsEditing))
            {
                await DialogService.ShowWarning("Primero guarde o cancele los detalles en edición", "Validación");
                return;
            }

            LoadingService.StartLoading();

            // Preparar el objeto Pes
            var pesaje = new Pes
            {
                pes_id = Pes_id,
                pes_des = Pes_des,
                pes_tipo = Pes_tipo,
                pes_baz_des = Pes_baz_des,
                pes_referencia = Pes_referencia,
                pes_fecha = Pes_fecha,
                pes_status = Pes_status,
                pes_mov_id = Pes_mov_id,
                pes_obs = Pes_obs,
                action = EsEdicion ? ActionType.Update.ToString() : ActionType.Create.ToString(),
                pdes = Detalles.Select(d => new Pde
                {
                    pde_id = d.Pde_id,
                    pde_pes_id = d.Pde_pes_id,
                    pde_mde_id = d.Pde_mde_id,
                    pde_bie_id = d.Pde_bie_id,
                    pde_nbza = d.Pde_nbza,
                    pde_pb = (float)d.Pde_pb,
                    pde_pt = (float)d.Pde_pt,
                    pde_pn = (float)d.Pde_pn,
                    pde_obs = d.Pde_obs,
                    pde_tipo = new[] { "PE", "DS" }.Contains(Pes_tipo) ? 2 : 1,
                    pde_t6m_id = d.Pde_t6m_id
                }).ToList()
            };

            var response = await _pesajesService.Pesajes(pesaje);

            if (response.status != 1)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "Error al guardar", "Error");
                return;
            }

            await DialogService.ShowSuccess(response.Meta?.msg ?? "Guardado exitosamente", "Éxito");

            // Cerrar ventana
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al guardar");
        }
        finally
        {
            LoadingService.StopLoading();
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

        RequestClose?.Invoke();
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
            Pde_pb = 0,
            Pde_pt = 0,
            Pde_pn = 0
        };

        Detalles.Insert(0, nuevoDetalle);
        DetalleSeleccionado = nuevoDetalle;
        ActualizarDetallesTable();
    }

    private async Task EditarDetalleAsync(PesajesDetalleItemDto? detalle)
    {
        if (detalle == null || !detalle.CanEdit) return;

        // Validar que no haya otro detalle en edición
        if (Detalles.Any(d => d.IsEditing && d != detalle))
        {
            await DialogService.ShowWarning("Primero guarde o cancele el otro detalle en edición", "Validación");
            return;
        }

        // Guardar valores originales antes de editar
        detalle.SaveOriginalValues();

        // Activar modo de edición
        detalle.IsEditing = true;
        DetalleSeleccionado = detalle;

        // Actualizar tabla para refrescar botones
        ActualizarDetallesTable();
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
            LoadingService.StartLoading();

            var pde = new Pde
            {
                pde_id = detalle.Pde_id,
                pde_pes_id = detalle.Pde_pes_id,
                action = ActionType.Delete.ToString()
            };

            var response = await _pesajesService.PesajesDetalle(pde);

            if (response.status != 1)
            {
                await DialogService.ShowError(response.Meta?.msg ?? "Error al eliminar", "Error");
                return;
            }

            Detalles.Remove(detalle);
            await DialogService.ShowSuccess("Detalle eliminado correctamente", "Éxito");
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

    private async Task GuardarDetalleAsync(PesajesDetalleItemDto? detalle)
    {
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

        if (detalle.Pde_pb <= 0)
        {
            await DialogService.ShowWarning("Ingrese el peso bruto", "Validación");
            return;
        }

        if (detalle.Pde_pt > detalle.Pde_pb)
        {
            await DialogService.ShowWarning("La tara no puede ser superior al peso bruto", "Validación");
            return;
        }

        try
        {
            LoadingService.StartLoading();

            var pde = new Pde
            {
                pde_id = detalle.Pde_id,
                pde_pes_id = Pes_id > 0 ? Pes_id : detalle.Pde_pes_id,
                pde_mde_id = detalle.Pde_mde_id,
                pde_bie_id = detalle.Pde_bie_id,
                pde_nbza = detalle.Pde_nbza,
                pde_pb = (float)detalle.Pde_pb,
                pde_pt = (float)detalle.Pde_pt,
                pde_pn = (float)detalle.Pde_pn,
                pde_obs = detalle.Pde_obs,
                pde_tipo = new[] { "PE", "DS" }.Contains(Pes_tipo) ? 2 : 1,
                pde_t6m_id = detalle.Pde_t6m_id,
                action = detalle.IsNew ? ActionType.Create.ToString() : ActionType.Update.ToString()
            };

            // TODO: Agregar fotos capturadas si existen
            // pde.files = detalle.FotosCapturas?.Select(f => ConvertirAFormFile(f)).ToList();

            var response = await _pesajesService.PesajesDetalle(pde);

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

            // Refrescar tabla
            ActualizarDetallesTable();

            await DialogService.ShowSuccess("Detalle guardado correctamente", "Éxito");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al guardar detalle");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    private async Task CancelarEdicionDetalleAsync(PesajesDetalleItemDto? detalle)
    {
        if (detalle == null) return;

        if (detalle.IsNew)
        {
            Detalles.Remove(detalle);
        }
        else
        {
            // Verificar si hay cambios
            if (detalle.HasChanges())
            {
                var confirmar = await DialogService.ShowConfirm(
                    "Tiene cambios sin guardar. ¿Desea descartarlos?",
                    "Confirmar");

                if (!confirmar) return;
            }

            // Restaurar valores originales
            detalle.RestoreOriginalValues();
            detalle.IsEditing = false;

            // Refrescar tabla
            ActualizarDetallesTable();
        }

        await Task.CompletedTask;
    }

    private async Task VerCapturasAsync(PesajesDetalleItemDto? detalle)
    {
        if (detalle == null || !detalle.HasImages)
        {
            await DialogService.ShowInfo("No hay imágenes capturadas", "Información");
            return;
        }

        try
        {
            LoadingService.StartLoading();

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
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar imágenes: {ex.Message}", "Error");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }
    private async Task CapturarB1Async()
    {
        await CapturarPesoAsync(PesoB1, NombreB1);
    }

    private async Task CapturarB2Async()
    {
        await CapturarPesoAsync(PesoB2, NombreB2);
    }

    private async Task CapturarPesoAsync(string? peso, string nombreBalanza)
    {
        if (string.IsNullOrEmpty(peso))
        {
            await DialogService.ShowWarning("No se ha capturado el peso de la balanza", "Advertencia");
            return;
        }

        if (!decimal.TryParse(peso, out decimal pesoBruto))
        {
            await DialogService.ShowWarning("El peso capturado no es válido", "Advertencia");
            return;
        }

        // Buscar detalle en edición o el último agregado
        var detalleEditable = Detalles.FirstOrDefault(d => d.IsEditing)
                           ?? Detalles.FirstOrDefault(d => d.IsNew);

        if (detalleEditable == null)
        {
            await DialogService.ShowWarning(
                "Por favor, seleccione una fila editable o agregue una nueva para ingresar el peso",
                "Advertencia");
            return;
        }

        // Asignar valores
        detalleEditable.Pde_pb = pesoBruto;
        detalleEditable.Pde_pt = 0;
        detalleEditable.Pde_nbza = nombreBalanza;

        // TODO: Capturar fotos desde cámaras
        // await CapturarFotosAsync(detalleEditable, nombreBalanza);

        await DialogService.ShowInfo($"Peso capturado: {pesoBruto} kg desde {nombreBalanza}", "Éxito");
    }

    private async Task BuscarDocumentoAsync(string? filtro)
    {
        // TODO: Implementar búsqueda de documentos para tipo DS
        await DialogService.ShowInfo("Búsqueda de documentos en desarrollo", "Información");
    }

    #endregion

    #region Métodos Auxiliares del DataTable

    /// <summary>
    /// Actualiza el DataTable con los datos actuales de Detalles
    /// </summary>
    private void ActualizarDetallesTable()
    {
        DetallesTable.SetData(Detalles);

        // Configurar totales para Peso Neto
        DetallesTable.ConfigureTotals(new[] { "Pde_pn" });

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

    #endregion
    public Action? RequestClose { get; set; }

    private async void IniciarLecturaBalanzas()
    {
        var sede = await _configService.GetSedeActivaAsync();
        if (sede != null && sede.Balanzas.Any())
        {
            // Configurar nombres de balanzas en la UI
            if (sede.Balanzas.Count > 0) NombreB1 = sede.Balanzas[0].Nombre;
            if (sede.Balanzas.Count > 1) NombreB2 = sede.Balanzas[1].Nombre;

            // Iniciar servicio
            _serialPortService.OnPesosLeidos += OnPesosLeidos;
            _serialPortService.IniciarLectura(sede.Balanzas);
        }
    }

    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        // Actualizar propiedades en el hilo de la UI
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var sede = await _configService.GetSedeActivaAsync();
            if (sede == null) return;

            foreach (var lectura in lecturas)
            {
                // Buscar qué balanza es (B1 o B2) por el puerto
                var balanza = sede.Balanzas.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanza != null)
                {
                    if (balanza.Nombre == NombreB1) PesoB1 = lectura.Value;
                    else if (balanza.Nombre == NombreB2) PesoB2 = lectura.Value;
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
