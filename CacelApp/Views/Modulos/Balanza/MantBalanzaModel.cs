using CacelApp.Services.Dialog;
using CacelApp.Services.Image;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.Form; // Para RadioOption
using CacelApp.Shared.Controls.ImageViewer;
using CacelApp.Shared.Controls.PdfViewer;
using CacelApp.Views.Modulos.Balanza.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Balanza.Entities;
using Core.Services.Configuration;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Enums;
using Infrastructure.Services.Balanza;
using Infrastructure.Services.Shared;
using Microsoft.AspNetCore.Http;
using System.Collections.ObjectModel;
using System.IO;

namespace CacelApp.Views.Modulos.Balanza;

/// <summary>
/// ViewModel para la ventana de mantenimiento de registros de Balanza
/// Implementa todas las validaciones y lógica de negocio del formulario
/// </summary>
public partial class MantBalanzaModel : ViewModelBase
{
    private readonly IBalanzaSearchService _balanzaSearchService;
    private readonly IBalanzaService _balanzaService;
    private readonly IBalanzaReportService _balanzaReportService;
    private readonly ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    private readonly ICameraService _cameraService;
    private readonly IConfigurationService _configurationService;
    private readonly ISerialPortService _serialPortService;
    private Window _window;
    private int _registroId;
    private Baz? _registroActual;
    private const string DialogIdentifier = "MantBalanzaDialogHost";

    /// <summary>
    /// Asigna la ventana propietaria (debe llamarse desde el code-behind)
    /// </summary>
    public void SetWindow(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    #region Propiedades Observables

    [ObservableProperty]
    private string titulo = "Mantenimiento Balanza";

    [ObservableProperty]
    private string subtitulo = "Registro de pesaje en balanza";

    [ObservableProperty]
    private string? baz_des;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardar))]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string? baz_veh_id;

    [ObservableProperty]
    private string? baz_ref;

    [ObservableProperty]
    private bool puedeEditarPlaca = true;

    // Vehículos
    [ObservableProperty]
    private ObservableCollection<VehiculoItemViewModel> vehiculos = new();

    // Tipo de Operación
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardar))]
    private int? baz_tipo;

    [ObservableProperty]
    private ObservableCollection<RadioOption> tiposOperacion = new();

    // Pesos
    [ObservableProperty]
    private string nombreBalanza = "BALANZA";

    [ObservableProperty]
    private decimal? pesoBalanza;

    [ObservableProperty]
    private decimal? baz_pb;

    [ObservableProperty]
    private decimal? baz_pt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardar))]
    private decimal? baz_pn;

    private decimal _pesoBrutoFijo = 0;

    // Tipo de Pago
    [ObservableProperty]
    private ObservableCollection<SelectOption> tiposPago = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarColaboradorInterno), nameof(MostrarConductor), nameof(PuedeGuardar))]
    private int? baz_t1m_id;

    partial void OnBaz_t1m_idChanged(int? value)
    {
        // Cargar colaboradores cuando se selecciona tipo de pago interno (23)
        if (value == 23)
        {
            _ = CargarColaboradoresInternosAsync();
        }
        else
        {
            ColaboradoresInternos.Clear();
            baz_col_id = null;
        }
    }

    [ObservableProperty]
    private decimal baz_monto;

    // Colaborador Interno
    public bool MostrarColaboradorInterno => baz_t1m_id == 23;

    [ObservableProperty]
    private ObservableCollection<SelectOption> colaboradoresInternos = new();

    [ObservableProperty]
    private int? baz_col_id;

    // Conductor
    public bool MostrarConductor => baz_t1m_id != 23;

    [ObservableProperty]
    private string? conductor;

    [ObservableProperty]
    private string? licencia;

    [ObservableProperty]
    private string? whatsAppCliente;

    // Información Adicional
    [ObservableProperty]
    private string? nombreTransportista;

    [ObservableProperty]
    private string? dniRucTransportista;

    [ObservableProperty]
    private string? baz_doc;

    [ObservableProperty]
    private string? guia;

    // Comprobante SUNAT
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiereDocumentoSunat), nameof(PuedeGuardar))]
    private int baz_t10;

    [ObservableProperty]
    private ObservableCollection<RadioOption> tiposComprobante = new();

    public bool RequiereDocumentoSunat => baz_t10 != 0; // 0 is NA

    [ObservableProperty]
    private string? numDocumentoSunat;

    [ObservableProperty]
    private string? baz_obs;

    // Estado
    [ObservableProperty]
    private bool esEdicion;

    [ObservableProperty]
    private bool estadoCamara;

    [ObservableProperty]
    private bool tieneFotos;

    // Imágenes capturadas temporalmente (en memoria)
    public List<MemoryStream> ImagenesCapturadas { get; private set; } = new();

    [ObservableProperty]
    private bool puedeImprimir;

    [ObservableProperty]
    private string textoBotonGuardar = "Guardar";

    public bool PuedeGuardar =>
        !string.IsNullOrWhiteSpace(baz_veh_id) &&
        VehiculoSeleccionado != null &&
        baz_tipo.HasValue &&
        baz_pn.HasValue &&
        baz_t1m_id.HasValue &&
        ValidarCamposAdicionales();

    private VehiculoItemViewModel? VehiculoSeleccionado =>
        Vehiculos.FirstOrDefault(v => v.EstaSeleccionado);

    #endregion

    #region Comandos

    public IAsyncRelayCommand CapturarPesoCommand { get; }
    public IAsyncRelayCommand GuardarCommand { get; }
    public IAsyncRelayCommand ImprimirCommand { get; }
    public IAsyncRelayCommand MostrarImagenesCommand { get; }
    public IAsyncRelayCommand NuevoCommand { get; }
    public IAsyncRelayCommand DestareCommand { get; }
    public IAsyncRelayCommand CancelarCommand { get; }
    public IRelayCommand CerrarCommand { get; }

    #endregion

    public MantBalanzaModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IBalanzaSearchService balanzaReadService,
        IBalanzaService balanzaWriteService,
        IBalanzaReportService balanzaReportService,
        ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService,
        ICameraService cameraService,
        IConfigurationService configurationService,
        ISerialPortService serialPortService) : base(dialogService, loadingService)
    {
        _window = null!;
        _balanzaSearchService = balanzaReadService ?? throw new ArgumentNullException(nameof(balanzaReadService));
        _balanzaService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        // Inicializar opciones de Operación
        tiposOperacion = new ObservableCollection<RadioOption>
        {
            new RadioOption { Label = "Externo", Value = 0 },
            new RadioOption { Label = "Despacho", Value = 1 },
            new RadioOption { Label = "Recepción", Value = 2 }
        };
        tiposComprobante = new ObservableCollection<RadioOption>
        {
            new RadioOption { Label = "N/A", Value = 0 },
            new RadioOption { Label = "Boleta", Value = 1 },
            new RadioOption { Label = "Factura", Value = 2 }
        };

        // Inicializar comandos
        CapturarPesoCommand = new AsyncRelayCommand(CapturarPesoAsync);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, () => PuedeGuardar);
        ImprimirCommand = new AsyncRelayCommand(ImprimirAsync, () => PuedeImprimir);
        MostrarImagenesCommand = new AsyncRelayCommand(MostrarImagenesAsync, () => TieneFotos);
        NuevoCommand = new AsyncRelayCommand(Nuevo);
        DestareCommand = new AsyncRelayCommand(DestareAsync);
        CancelarCommand = new AsyncRelayCommand(CancelarAsync);
        CerrarCommand = new RelayCommand(() =>
        {
            Cleanup();
            _window.Close();
        });

        // Suscribirse a cambios de selección de vehículos
        Vehiculos.CollectionChanged += (s, e) =>
        {
            foreach (var vehiculo in Vehiculos)
            {
                vehiculo.PropertyChanged += (vs, ve) =>
                {
                    if (ve.PropertyName == nameof(VehiculoItemViewModel.EstaSeleccionado))
                    {
                        if (vehiculo.EstaSeleccionado)
                        {
                            baz_monto = vehiculo.Precio;

                            foreach (var v in Vehiculos.Where(v => v != vehiculo))
                                v.EstaSeleccionado = false;
                        }
                        GuardarCommand.NotifyCanExecuteChanged();
                    }
                };
            }
        };

        // No cargar datos aquí, se cargarán desde el evento Loaded de la ventana
    }
    private async void IniciarLecturaBalanzas()
    {
        try
        {
            var sede = await _configurationService.GetSedeActivaAsync();
            if (sede != null && sede.Balanzas.Any())
            {
                // Iniciar servicio
                _serialPortService.OnPesosLeidos += OnPesosLeidos;
                _serialPortService.IniciarLectura(sede.Balanzas, sede.Tipo);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al iniciar balanza: {ex.Message}");
        }
    }
    private void OnPesosLeidos(Dictionary<string, string> lecturas)
    {
        // Actualizar propiedades en el hilo de la UI
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var sede = await _configurationService.GetSedeActivaAsync();
            if (sede == null) return;

            // Buscar la balanza activa
            var balanzaActiva = sede.Balanzas.FirstOrDefault(b => b.Activa);

            if (balanzaActiva != null && lecturas.ContainsKey(balanzaActiva.Puerto))
            {
                if (decimal.TryParse(lecturas[balanzaActiva.Puerto], out decimal peso))
                {
                    PesoBalanza = peso;
                }
            }
        });
    }

    public void Cleanup()
    {
        try
        {
            _serialPortService.DetenerLectura();
            _serialPortService.OnPesosLeidos -= OnPesosLeidos;
        }
        catch { }
    }
    #region Métodos Privados

    public async Task CargarDatosInicialesAsync()
    {
        try
        {
            LoadingService.StartLoading();
            CargarVehiculos();
            await CargarTiposPagoAsync();

            if (!EsEdicion)
            {
                baz_tipo = 0;  // CompraExterna
                baz_t1m_id = 9; // Contado por defecto
                baz_t10 = 0; // N/A
            }

            await Task.CompletedTask;
            IniciarLecturaBalanzas();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al cargar datos iniciales", dialogIdentifier: DialogIdentifier);
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    private void CargarVehiculos()
    {
        // Limpiar vehículos existentes
        Vehiculos.Clear();
        var vehiculosData = new[]
        {
            new { Id = 1, Neje = 1, Nombre = "1 a 3 TN", Precio = 5m, Capacidad = "1 a 3 TN", Imagen = "truck_1.png" },
            new { Id = 2, Neje = 2, Nombre = "1 a 6 TN", Precio = 7m, Capacidad = "1 a 6 TN", Imagen = "truck_2.jpg" },
            new { Id = 3, Neje = 3, Nombre = "1 a 10 TN", Precio = 8m, Capacidad = "1 a 10 TN", Imagen = "truck_3.png" },
            new { Id = 4, Neje = 4, Nombre = "1 a 12 TN", Precio = 10m, Capacidad = "1 a 12 TN", Imagen = "truck_4.png" },
            new { Id = 5, Neje = 5, Nombre = "1 a 15 TN", Precio = 12m, Capacidad = "1 a 15 TN", Imagen = "truck_5.png" },
            new { Id = 6, Neje = 6, Nombre = "1 a 20 TN", Precio = 15m, Capacidad = "1 a 20 TN", Imagen = "truck_6.png" },
            new { Id = 7, Neje = 7, Nombre = "1 a 30 TN", Precio = 20m, Capacidad = "1 a 30 TN", Imagen = "truck_7.png" },
            new { Id = 8, Neje = 8, Nombre = "1 a 40 TN", Precio = 25m, Capacidad = "1 a 40 TN", Imagen = "truck_8.png" }
        };

        foreach (var vehiculo in vehiculosData)
        {
            var nuevoVehiculo = new VehiculoItemViewModel
            {
                Id = vehiculo.Neje,  // Usar Neje como ID (número de ejes)
                Nombre = vehiculo.Nombre,
                Precio = vehiculo.Precio,
                Capacidad = vehiculo.Capacidad,
                ImagenUrl = $"pack://application:,,,/Assets/Image/trucks/{vehiculo.Imagen}",
                EstaSeleccionado = false
            };

            // Suscribir evento ANTES de agregar a la colección
            SuscribirEventoVehiculo(nuevoVehiculo);

            Vehiculos.Add(nuevoVehiculo);
        }
    }

    /// <summary>
    /// Suscribe el evento PropertyChanged de un vehículo para actualizar el monto cuando se selecciona
    /// </summary>
    private void SuscribirEventoVehiculo(VehiculoItemViewModel vehiculo)
    {
        vehiculo.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(VehiculoItemViewModel.EstaSeleccionado))
            {
                if (vehiculo.EstaSeleccionado)
                {
                    // Actualizar monto cuando se selecciona un vehículo
                    Baz_monto = vehiculo.Precio;

                    // Deseleccionar otros
                    foreach (var v in Vehiculos.Where(v => v != vehiculo))
                        v.EstaSeleccionado = false;
                }

                // Siempre notificar que cambió la condición de guardado
                GuardarCommand.NotifyCanExecuteChanged();
            }
        };
    }

    private async Task CargarTiposPagoAsync()
    {
        try
        {
            TiposPago.Clear();
            var tiposPago = await _selectOptionService.GetSelectOptionsAsync(SelectOptionType.TipoPago);

            foreach (var tipo in tiposPago)
            {
                TiposPago.Add(tipo);
            }
            if (!TiposPago.Any())
            {
                await DialogService.ShowInfo(
                    "No se pudieron cargar los tipos de pago desde el servidor.",
                    "Advertencia"
                , dialogIdentifier: DialogIdentifier);
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(
                $"Error al cargar tipos de pago: {ex.Message}",
                "Error"
            , dialogIdentifier: DialogIdentifier);

        }
    }

    private async Task CargarColaboradoresInternosAsync()
    {
        try
        {
            ColaboradoresInternos.Clear();

            // Cargar colaboradores del puesto 3 (colaboradores internos)
            var colaboradores = await _selectOptionService.GetSelectOptionsAsync(
                SelectOptionType.Colaborador,
                code: 3
            );

            foreach (var colaborador in colaboradores)
            {
                ColaboradoresInternos.Add(colaborador);
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(
                $"Error al cargar colaboradores: {ex.Message}",
                "Error"
            , dialogIdentifier: DialogIdentifier);
        }
    }

    private bool ValidarCamposAdicionales()
    {
        // Si es tipo de pago específico, validar WhatsApp
        if (baz_t1m_id == 6 && string.IsNullOrWhiteSpace(WhatsAppCliente))
            return false;

        // Si requiere documento SUNAT, validar
        if (RequiereDocumentoSunat && string.IsNullOrWhiteSpace(NumDocumentoSunat))
            return false;

        // Si es colaborador interno, validar selección
        if (MostrarColaboradorInterno && !baz_col_id.HasValue)
            return false;

        return true;
    }

    private async Task<bool> ValidarFormularioAsync()
    {
        // Validar vehículo seleccionado
        if (VehiculoSeleccionado == null)
        {
            await DialogService.ShowWarning("Debe seleccionar un vehículo", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar placa
        if (string.IsNullOrWhiteSpace(baz_veh_id))
        {
            await DialogService.ShowWarning("Debe ingresar una placa", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        if (baz_veh_id.Length < 6)
        {
            await DialogService.ShowWarning("La placa debe tener al menos 6 caracteres", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        if (baz_veh_id.Length > 8)
        {
            await DialogService.ShowWarning("La placa debe tener máximo 8 caracteres", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar tipo de operación
        if (!baz_tipo.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un tipo de operación", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar pesos
        if (!baz_pb.HasValue || !baz_pt.HasValue || !baz_pn.HasValue)
        {
            await DialogService.ShowWarning("Debe capturar el peso de la balanza", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar tipo de pago
        if (!baz_t1m_id.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un tipo de pago", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar WhatsApp si es necesario
        if (baz_t1m_id == 6 && string.IsNullOrWhiteSpace(WhatsAppCliente))
        {
            await DialogService.ShowWarning("Debe ingresar el WhatsApp del cliente", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar documento SUNAT
        if (baz_t10 == 1) // Boleta
        {
            if (string.IsNullOrWhiteSpace(NumDocumentoSunat))
            {
                await DialogService.ShowWarning("Debe ingresar el DNI para boleta", "Validación", dialogIdentifier: DialogIdentifier);
                return false;
            }

            if (NumDocumentoSunat.Length != 8 && !NumDocumentoSunat.StartsWith("10"))
            {
                await DialogService.ShowWarning("Debe ingresar un DNI válido (8 dígitos)", "Validación");
                return false;
            }
        }
        else if (baz_t10 == 2) // Factura
        {
            if (string.IsNullOrWhiteSpace(NumDocumentoSunat))
            {
                await DialogService.ShowWarning("Debe ingresar el RUC para factura", "Validación", dialogIdentifier: DialogIdentifier);
                return false;
            }

            if (NumDocumentoSunat.Length != 11)
            {
                await DialogService.ShowWarning("Debe ingresar un RUC válido (11 dígitos)", "Validación");
                return false;
            }
        }

        // Validar colaborador interno si es necesario
        if (MostrarColaboradorInterno && !baz_col_id.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un colaborador interno", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        return true;
    }

    #endregion

    #region Comandos Implementation

    private async Task CapturarPesoAsync()
    {
        try
        {
            if (!PesoBalanza.HasValue || PesoBalanza <= 0)
            {
                await DialogService.ShowWarning("No se ha capturado el peso de la balanza.\nAsegúrese de que la balanza esté conectada y transmitiendo.", "Captura de Peso", dialogIdentifier: DialogIdentifier);
                return;
            }

            var pesoActual = PesoBalanza.Value;
            if (!EsEdicion)
            {
                // Primera captura - modo CREATE
                Baz_pb = pesoActual;  // Usar propiedad pública para notificar cambios
                Baz_pt = 0;           // Usar propiedad pública para notificar cambios
                _pesoBrutoFijo = pesoActual;
                // Status = 1 (pesado una vez)
            }
            else
            {
                // Segunda captura - modo UPDATE (destare)
                _pesoBrutoFijo = Baz_pb.Value; // Guardar el peso bruto original

                if (pesoActual > _pesoBrutoFijo)
                {
                    // El peso actual es mayor que el bruto anterior
                    // El bruto anterior se convierte en tara
                    Baz_pt = _pesoBrutoFijo;  // Usar propiedad pública para notificar cambios
                    Baz_pb = pesoActual;      // Usar propiedad pública para notificar cambios
                    _pesoBrutoFijo = pesoActual;
                    // baz_order = 1 (bruto después)
                }
                else
                {
                    // El peso actual es menor que el bruto anterior
                    // El peso actual es la tara
                    Baz_pb = _pesoBrutoFijo;  // Usar propiedad pública para notificar cambios
                    Baz_pt = pesoActual;      // Usar propiedad pública para notificar cambios
                    // baz_order = 0 (bruto primero)
                }
                // Status = 2 (pesado dos veces, completo)
            }

            // Calcular peso neto
            Baz_pn = Baz_pb.Value - (Baz_pt ?? 0);  // Usar propiedades públicas para notificar cambios

            // Capturar fotos de cámaras
            await CapturarFotosCamarasAsync();
            TieneFotos = ImagenesCapturadas.Any();

            GuardarCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al capturar peso", dialogIdentifier: DialogIdentifier);
        }
    }

    private async Task GuardarAsync()
    {
        try
        {
            // Validar formulario completo
            if (!await ValidarFormularioAsync())
                return;

            // Confirmar si es edición y hay alertas
            if (EsEdicion)
            {
                string alertas = string.Empty;

                // Validar si peso bruto == peso neto (tara en 0)
                if (baz_pb == baz_pn)
                {
                    alertas += "⚠️ Se detectó igualdad entre peso bruto y peso neto (tara en 0)\n";
                }

                if (!string.IsNullOrEmpty(alertas))
                {
                    var confirmar = await DialogService.ShowConfirm(
                        "Confirmación de Actualización",
                        $"¿Está seguro de actualizar el registro N° {baz_des}?\n\n" +
                        $"Peso Bruto: {baz_pb:N2} kg\n" +
                        $"Peso Tara: {baz_pt:N2} kg\n" +
                        $"Peso Neto: {baz_pn:N2} kg\n\n" +
                        alertas,
                        dialogIdentifier: DialogIdentifier);

                    if (!confirmar)
                        return;
                }
            }

            LoadingService.StartLoading();

            // Preparar entidad Baz para guardar
            var registro = PrepararRegistroParaGuardar();

            // Llamar al servicio según si es creación o actualización
            Baz resultado;
            if (EsEdicion && _registroId > 0)
            {
                registro.action = ActionType.Update.ToString();
                registro.baz_id = _registroId;
                resultado = await _balanzaService.Balanza(registro);
            }
            else
            {
                registro.action = ActionType.Create.ToString();
                resultado = await _balanzaService.Balanza(registro);
                _registroId = resultado.baz_id;
            }

            // Actualizar NTicket con el valor devuelto
            baz_des = resultado.baz_des;

            // Actualizar estado de la UI
            EsEdicion = true;
            PuedeEditarPlaca = false;
            TextoBotonGuardar = "Actualizar";
            PuedeImprimir = true;

            await DialogService.ShowSuccess(
                "Éxito",
                EsEdicion ?
                    $"Registro {baz_des} actualizado correctamente" :
                    $"Registro {baz_des} guardado correctamente", dialogIdentifier: DialogIdentifier);

            // Cerrar ventana con resultado exitoso
            //_window.DialogResult = true;
            //_window.Close();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(
                $"Ocurrió un error al guardar el registro:\n{ex.Message}",
                "Error al guardar", dialogIdentifier: DialogIdentifier);
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    /// <summary>
    /// Prepara la entidad Baz con todos los datos del formulario
    /// </summary>
    private Baz PrepararRegistroParaGuardar()
    {
        var vehiculoSel = VehiculoSeleccionado;

        return new Baz
        {
            baz_id = _registroId,
            baz_veh_id = baz_veh_id?.ToUpper() ?? string.Empty,
            baz_ref = baz_ref,
            baz_tipo = (int?)baz_tipo,  // 0, 1 o 2
            baz_pb = baz_pb,
            baz_pt = baz_pt,
            baz_pn = baz_pn,
            baz_t1m_id = baz_t1m_id,
            baz_monto = baz_monto,
            baz_col_id = baz_col_id,
            baz_doc = baz_doc,
            baz_obs = baz_obs,
            baz_t10 = (int)baz_t10,
            baz_status = EsEdicion ? 2 : 1, // 1 = primera pesada, 2 = segunda pesada (completo)
            baz_order = 0, // Se define en la lógica de captura
            veh_veh_neje = vehiculoSel?.Id,

            // Transportista
            tra = new Tra
            {
                age_des = NombreTransportista,
                age_nro = DniRucTransportista
            },

            // Conductor (Guide Person)
            gpe = new Gpe
            {
                gpe_nombre = Conductor,
                gpe_identificacion = Licencia
            },

            // Agencia/Cliente SUNAT
            age = new Age
            {
                age_telefono = WhatsAppCliente,
                age_nro = NumDocumentoSunat
            },

            // Vehículo seleccionado
            veh = vehiculoSel != null ? new Veh
            {
                veh_neje = vehiculoSel.Id,
                veh_obs = vehiculoSel.Nombre,
                veh_ref = (int?)vehiculoSel.Precio
            } : null,

            // TODO: Agregar fotos capturadas
            // Agregar fotos capturadas
            files = ImagenesCapturadas.Select((ms, index) =>
            {
                var bytes = ms.ToArray();
                return (IFormFile)new SimpleFormFile(bytes, "files", $"{index + 1}.jpg");
            }).ToList()
        };
    }

    private async Task ImprimirAsync()
    {
        try
        {
            LoadingService.StartLoading();

            var pdfBytes = await _balanzaReportService.GenerarReportePdfAsync(_registroActual.baz_id);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                await DialogService.ShowWarning("Sin datos", "No se pudo generar el reporte PDF");
                return;
            }

            if (pdfBytes.Length > 0)
            {
                var pdfWindow = new PdfViewerWindow(pdfBytes, $"Reporte {baz_des}");
                pdfWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al generar reporte", dialogIdentifier: DialogIdentifier);
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    private async Task MostrarImagenesAsync()
    {
        try
        {
            if (_registroActual == null || string.IsNullOrEmpty(_registroActual.baz_media))
            {
                await DialogService.ShowInfo("El registro no tiene capturas de cámara registradas", "Sin imágenes", dialogIdentifier: DialogIdentifier);
                return;
            }

            LoadingService.StartLoading();

            var bazMedia = _registroActual.baz_media ?? string.Empty;
            var bazMedia1 = _registroActual.baz_media1 ?? string.Empty;

            // Cargar imágenes de pesaje
            var imagenesPesaje = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia) && !string.IsNullOrEmpty(_registroActual.baz_path))
            {
                imagenesPesaje = await _imageLoaderService.CargarImagenesAsync(
                    _registroActual.baz_path,
                    bazMedia);
            }

            // Cargar imágenes de destare
            var imagenesDestare = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
            if (!string.IsNullOrEmpty(bazMedia1) && !string.IsNullOrEmpty(_registroActual.baz_path))
            {
                imagenesDestare = await _imageLoaderService.CargarImagenesAsync(
                    _registroActual.baz_path,
                    bazMedia1);
            }

            LoadingService.StopLoading();

            // Verificar si se cargaron imágenes
            if (!imagenesPesaje.Any() && !imagenesDestare.Any())
            {
                await DialogService.ShowWarning("No se pudieron cargar las imágenes del registro", "Sin imágenes", dialogIdentifier: DialogIdentifier);
                return;
            }

            // Crear ViewModel y mostrar ventana
            var viewModel = new ImageViewerViewModel(
                imagenesPesaje,
                imagenesDestare.Any() ? imagenesDestare : null,
                $"Registro: {baz_des} - Placa: {baz_veh_id}");

            var imageViewer = new ImageViewerWindow(viewModel);
            imageViewer.ShowDialog();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"No se pudieron cargar las imágenes: {ex.Message}", "Error", dialogIdentifier: DialogIdentifier);
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    private async Task CancelarAsync()
    {
        // Preguntar confirmación si hay cambios
        if (EsEdicion || !string.IsNullOrEmpty(baz_des) || baz_pb > 0)
        {
            var resultado = await DialogService.ShowConfirm(
                "Confirmar Cancelación",
                "¿Está seguro de cancelar? Se perderán los cambios no guardados.",
                "Sí",
                "No",
                dialogIdentifier: DialogIdentifier
            );

            if (!resultado) return;
        }

        // Limpiar el formulario
        Nuevo();
        Titulo = "Mantenimiento de Balanza";
        Subtitulo = "Agregar nuevo registro de pesaje";

        // Actualizar comandos
        GuardarCommand.NotifyCanExecuteChanged();
        ImprimirCommand.NotifyCanExecuteChanged();

        await DialogService.ShowInfo("Formulario limpiado. Puede ingresar un nuevo registro.", "Nuevo Registro", dialogIdentifier: DialogIdentifier);
    }

    private async Task Nuevo()
    {
        // Limpiar todos los campos del formulario
        _registroId = 0;
        PesoBalanza = 0;
        Baz_pb = 0;
        Baz_pt = 0;
        Baz_pn = 0;
        _pesoBrutoFijo = 0;

        // Limpiar selecciones de vehículos
        foreach (var vehiculo in Vehiculos)
        {
            vehiculo.EstaSeleccionado = false;
        }

        // Valores por defecto (Legacy Logic)
        Baz_t1m_id = 9; // Contado
        Baz_col_id = null;

        // Limpiar campos de texto
        Baz_des = string.Empty;
        Baz_veh_id = string.Empty;
        Baz_monto = 0;
        Baz_ref = string.Empty;
        Baz_obs = string.Empty;
        WhatsAppCliente = string.Empty;
        NumDocumentoSunat = string.Empty;
        Conductor = string.Empty;
        Licencia = string.Empty;
        NombreTransportista = string.Empty;
        DniRucTransportista = string.Empty;
        Baz_doc = string.Empty;
        Guia = string.Empty;

        // Resetear tipo de operación y comprobante
        Baz_tipo = 0; // CompraExterna
        Baz_t10 = 0; // NA

        // Resetear estado de botones
        EsEdicion = false;
        PuedeEditarPlaca = true;
        TextoBotonGuardar = "Guardar";
        PuedeImprimir = false;
        Titulo = "Mantenimiento de Balanza";
        Subtitulo = "Agregar nuevo registro de pesaje";

        // Actualizar comandos
        GuardarCommand.NotifyCanExecuteChanged();
        ImprimirCommand.NotifyCanExecuteChanged();

    }

    private async Task DestareAsync()
    {
        try
        {
            var window = new DestareVehiculos(new DestareVehiculosModel(DialogService, LoadingService, _balanzaSearchService)) { Owner = _window };
            var result = window.ShowDialog();

            if (result == true && window.RegistroSeleccionado != null)
            {
                CargarRegistroCompleto(window.RegistroSeleccionado);
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar el registro para destare: {ex.Message}", "Error", dialogIdentifier: DialogIdentifier);
        }
    }
    #endregion

    /// <summary>
    /// Cargar registro desde DTO de la lista (usado cuando no hay servicio GetById)
    /// </summary>
    /// <summary>
    /// Cargar registro completo con todos los datos desde la entidad Baz (usado en edición)
    /// </summary>
    public void CargarRegistroCompleto(Baz baz)
    {
        if (baz == null) return;

        // Guardar registro actual para acceder a datos adicionales (imágenes, etc.)
        _registroActual = baz;

        _registroId = baz.baz_id;
        EsEdicion = true;
        PuedeImprimir = true;
        PuedeEditarPlaca = false;
        TextoBotonGuardar = "Actualizar";
        Titulo = "Editar Registro de Balanza";
        Subtitulo = $"Modificando registro {baz.baz_des}";

        // Datos básicos
        Baz_des = baz.baz_des;
        Baz_veh_id = baz.baz_veh_id;
        Baz_ref = baz.baz_ref;

        // Tipo de operación
        if (baz.baz_tipo.HasValue)
        {
            Baz_tipo = baz.baz_tipo.Value;
        }

        // Pesos
        Baz_pb = baz.baz_pb;
        Baz_pt = baz.baz_pt;
        Baz_pn = baz.baz_pn;
        _pesoBrutoFijo = baz.baz_pb ?? 0;

        // Tipo de pago y monto
        Baz_t1m_id = baz.baz_t1m_id;
        Baz_monto = baz.baz_monto;

        // Transportista
        if (baz.tra != null)
        {
            NombreTransportista = baz.tra.age_des;
            DniRucTransportista = baz.tra.age_nro;
        }

        // Conductor
        if (baz.gpe != null)
        {
            Conductor = baz.gpe.gpe_nombre;
            Licencia = baz.gpe.gpe_identificacion;
        }

        // Colaborador interno
        Baz_col_id = baz.baz_col_id;

        // Documentos
        Baz_doc = baz.baz_doc;
        Baz_obs = baz.baz_obs;

        // Comprobante SUNAT
        if (baz.baz_t10.HasValue)
        {
            Baz_t10 = baz.baz_t10.Value;
        }

        if (baz.age != null)
        {
            WhatsAppCliente = baz.age.age_telefono;
            NumDocumentoSunat = baz.age.age_nro;
        }

        // Verificar si tiene imágenes
        TieneFotos = !string.IsNullOrEmpty(baz.baz_media) || !string.IsNullOrEmpty(baz.baz_media1);
        MostrarImagenesCommand.NotifyCanExecuteChanged();

        // Seleccionar vehículo después de que se carguen (se hará cuando se inicialice)
        if (baz.veh != null && baz.veh.veh_neje.HasValue)
        {
            var vehiculo = Vehiculos.FirstOrDefault(v => v.Id == baz.veh.veh_neje.Value);
            if (vehiculo != null)
            {
                vehiculo.EstaSeleccionado = true;
            }
        }
    }


    private async Task CapturarFotosCamarasAsync()
    {
        try
        {
            ImagenesCapturadas.Clear();

            // 1. Obtener configuración de la sede activa
            var sede = await _configurationService.GetSedeActivaAsync();
            if (sede == null || !sede.RequiereCamaras()) return;

            // 2. Obtener la balanza activa (asumimos la primera por ahora o la que coincida con el nombre si tuviéramos esa info)
            var balanzaConfig = sede.Balanzas.FirstOrDefault(b => b.Activa);
            if (balanzaConfig == null || !balanzaConfig.CanalesCamaras.Any()) return;

            // 3. Inicializar servicio de cámaras si es necesario
            if (!await _cameraService.InicializarAsync(sede.Dvr, sede.Camaras.ToList()))
            {
                return;
            }

            // 4. Capturar imágenes de los canales asociados
            foreach (var canal in balanzaConfig.CanalesCamaras)
            {
                try
                {
                    var imagenStream = await _cameraService.CapturarImagenAsync(canal);
                    if (imagenStream != null)
                    {
                        ImagenesCapturadas.Add(imagenStream);
                    }
                }
                catch
                {
                    // Ignorar errores individuales
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error capturando fotos: {ex.Message}");
        }
    }
}
