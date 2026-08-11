using CacelApp.Services.Dialog;
using CacelApp.Services.Image;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.Form;
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
using Microsoft.Win32;
using Services.Shared;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text.Json;
using System.Windows.Markup;
using static System.Windows.Forms.DataFormats;

namespace CacelApp.Views.Modulos.Balanza;

/// <summary>
/// ViewModel para la ventana de mantenimiento de registros de Balanza
/// Implementa todas las validaciones y lógica de negocio del formulario
/// </summary>
public partial class MantBalanzaModel : ViewModelBase
{
    private readonly IBalanzaSearchService _balanzaSearchService;
    private readonly IBalanzaService _balanzaService;
    private readonly IFindFileService _findFileService;
    private readonly ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    private readonly ICameraService _cameraService;
    private readonly IConfigurationService _configurationService;
    private readonly ISerialPortService _serialPortService;
    private readonly CacelApp.Services.ImageAudit.IImageAuditService _imageAuditService;
    private Window _window;
    private int _registroId;
    private Baz? _registroActual;
    private const string DialogIdentifier = "MantBalanzaDialogHost";
    private bool showDestareConfirm =false;
    private int? status=0;
    /// <summary>
    /// Asigna la ventana propietaria (debe llamarse desde el code-behind)
    /// </summary>
    public void SetWindow(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <summary>
    /// Evento que se dispara cuando se guarda exitosamente un registro
    /// </summary>
    public event EventHandler? OnSaved;

    #region Propiedades Observables

    [ObservableProperty]
    private string titulo = "Mantenimiento Balanza";

    [ObservableProperty]
    private string subtitulo = "Registro de pesaje en balanza";

    [ObservableProperty]
    private string? baz_des;

    [ObservableProperty]
    private int? baz_nro;

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
    private int? baz_tipo = 0;

    partial void OnBaz_tipoChanged(int? value)
    {
        if (value == 2)
        {
            Baz_t1m_id = 22;
        }
        else
        {
            Baz_t1m_id = 9;
        }
    }

    [ObservableProperty]
    private ObservableCollection<RadioOption> tiposOperacion = new();

    // Pesos
    [ObservableProperty]
    private ObservableCollection<CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo> balanzasInfo = new();

    public CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo? PrimeraBalanza =>
        BalanzasInfo.FirstOrDefault();

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
            Conductor = string.Empty;
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
        baz_t1m_id.HasValue;


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
    public IRelayCommand ReiniciarSerialCommand { get; }

    #endregion

    public MantBalanzaModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IBalanzaSearchService balanzaReadService,
        IBalanzaService balanzaWriteService,
        IFindFileService findFileService,
        ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService,
        ICameraService cameraService,
        IConfigurationService configurationService,
        ISerialPortService serialPortService,
        CacelApp.Services.ImageAudit.IImageAuditService imageAuditService) : base(dialogService, loadingService)
    {
        _window = null!;
        _balanzaSearchService = balanzaReadService ?? throw new ArgumentNullException(nameof(balanzaReadService));
        _balanzaService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _findFileService = findFileService ?? throw new ArgumentNullException(nameof(findFileService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
        _imageAuditService = imageAuditService ?? throw new ArgumentNullException(nameof(imageAuditService));
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
        CapturarPesoCommand = SafeCommand(CapturarPesoAsync);
        GuardarCommand = new AsyncRelayCommand(() => ExecuteSafeAsync(GuardarAsync), () => PuedeGuardar);
        ImprimirCommand = new AsyncRelayCommand(() => ExecuteSafeAsync(ImprimirAsync), () => PuedeImprimir);
        MostrarImagenesCommand = new AsyncRelayCommand(() => ExecuteSafeAsync(MostrarImagenesAsync), () => TieneFotos);
        NuevoCommand = SafeCommand(Nuevo);
        DestareCommand = SafeCommand(DestareAsync);
        CancelarCommand = SafeCommand(CancelarAsync);
        CerrarCommand = new RelayCommand(() =>
        {
            Cleanup();
            _window.Close();
        });
        ReiniciarSerialCommand = new RelayCommand(() =>
        {
            Cleanup();
            IniciarLecturaBalanzas();
        });        
    }
    private Dictionary<string, string> _balanzaPuertoMap = new();

    private async void IniciarLecturaBalanzas()
    {
        try
        {
            var sede = await _configurationService.GetSedeActivaAsync();
            if (sede != null && sede.Balanzas.Any())
            {
                BalanzasInfo.Clear();
                _balanzaPuertoMap.Clear();
                var balanza = sede.Balanzas.First();

                if (!string.IsNullOrEmpty(balanza.Puerto))
                {
                    _balanzaPuertoMap[balanza.Puerto] = balanza.Nombre;
                }

                var balanzaInfo = new CacelApp.Shared.Controls.WeightDisplay.BalanzaDisplayInfo
                {
                    Nombre = balanza.Nombre,
                    Puerto = balanza.Puerto,
                    Conectada = balanza.Conectada,
                    MostrarBotonCaptura = true,
                    CapturarCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                            await CapturarPesoAsync()))
                };

                BalanzasInfo.Add(balanzaInfo);
                OnPropertyChanged(nameof(PrimeraBalanza));

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al iniciar balanza: {ex.Message}");
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
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var lectura in lecturas)
            {
                var balanzaInfo = BalanzasInfo.FirstOrDefault(b => b.Puerto == lectura.Key);
                if (balanzaInfo != null && decimal.TryParse(lectura.Value, out decimal peso))
                {
                    balanzaInfo.PesoActual = peso;
                    balanzaInfo.Conectada = true;
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
            _serialPortService.OnEstabilidadCambiada -= OnEstabilidadCambiada;
        }
        catch { }
    }
    #region Métodos Privados

    public async Task CargarDatosInicialesAsync()
    {
        try
        {
            Cleanup();
            LoadingService.StartLoading();
            await CargarVehiculosAsync();
            await CargarTiposPagoAsync();
           
            if (!EsEdicion)
            {
                Baz_pb = 0;  
                Baz_pt = 0;  
                Baz_pn = 0;  
                Baz_tipo = 0;  // CompraExterna
                Baz_t1m_id = 9; // Contado por defecto
                Baz_t10 = 0; // N/A
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

    private async Task CargarVehiculosAsync()
    {
        Vehiculos.Clear();

        var opciones = await _selectOptionService.GetSelectOptionsAsync(SelectOptionType.Vehiculos);
        var count = 1;
        foreach (var opcion in opciones)
        {
            var veh = opcion.Ext as Core.Shared.Entities.Generic.Veh;

            var nuevoVehiculo = new VehiculoItemViewModel
            {
                Id = veh?.veh_id,
                Nombre = veh?.veh_year?.ToString() ?? string.Empty,
                Precio = veh?.veh_ref.HasValue == true ? (decimal)veh.veh_ref.Value : 0m,
                Capacidad = veh?.veh_year?.ToString() ?? string.Empty,
                ImagenUrl = $"pack://application:,,,/Assets/Image/trucks/truck_{count}.png",
                EstaSeleccionado = false,
                Veh=veh
            };

            SuscribirEventoVehiculo(nuevoVehiculo);
            Vehiculos.Add(nuevoVehiculo);
            count++;
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
                    Baz_monto = vehiculo.Precio;

                    foreach (var v in Vehiculos.Where(v => v != vehiculo))
                        v.EstaSeleccionado = false;
                }

                OnPropertyChanged(nameof(PuedeGuardar));
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


    private async Task<bool> ValidarFormularioAsync()
    {
        if (showDestareConfirm)
        {
            var continuar = await DialogService.ShowConfirm(
            "Este registro ya tiene un destare realizado. ¿Desea continuar y realizar el destare nuevamente?",
            "Confirmación",
            "Continuar");

            if (!continuar)
            {
                return false;
            }
        }
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

        //// Validar WhatsApp si es necesario
        //if (baz_t1m_id == 6 && string.IsNullOrWhiteSpace(WhatsAppCliente))
        //{
        //    await DialogService.ShowWarning("Debe ingresar el WhatsApp del cliente", "Validación", dialogIdentifier: DialogIdentifier);
        //    return false;
        //}

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
        var balanza = BalanzasInfo.FirstOrDefault();
        if (balanza == null || !balanza.PesoActual.HasValue || balanza.PesoActual <= 0)
        {
            await DialogService.ShowWarning("No se ha capturado el peso de la balanza.\nAsegúrese de que la balanza esté conectada y transmitiendo.", "Captura de Peso", dialogIdentifier: DialogIdentifier);
            return;
        }
       
        var pesoActual = balanza.PesoActual.Value;
        if (!EsEdicion )
        {

            Baz_pb = pesoActual;
            Baz_pt = 0;
            _pesoBrutoFijo = pesoActual;
        }
        else 
        {
            _pesoBrutoFijo = _registroActual.baz_order == 1 ? Baz_pt.Value : Baz_pb.Value;
            showDestareConfirm = status == 2;
            if (pesoActual > _pesoBrutoFijo)
            {
                Baz_pt = _pesoBrutoFijo;
                Baz_pb = pesoActual;
                _pesoBrutoFijo = pesoActual;
                _registroActual.baz_order = 1;
                _registroActual.baz_fecha = DateTime.Now;
                _registroActual.baz_status = 2;
            }
            else
            {
                Baz_pb = _pesoBrutoFijo;
                Baz_pt = pesoActual;
                _registroActual.baz_order = 0;
                _registroActual.baz_fecha = DateTime.Now;
                _registroActual.baz_status = 2;
            }
       
        }

        Baz_pn = Baz_pb.Value - (Baz_pt ?? 0);
        await CapturarFotosCamarasAsync();
        TieneFotos = ImagenesCapturadas.Any();

        GuardarCommand.NotifyCanExecuteChanged();

    }

    private async Task GuardarAsync()
    {

        if (!await ValidarFormularioAsync())
            return;
        var registro = PrepararRegistroParaGuardar();
        Baz resultado;
        if (EsEdicion && _registroId > 0)
        {
            registro.action = ActionType.Update;
            registro.baz_id = _registroId;
            registro.baz_nro = Baz_nro;
            resultado = await _balanzaService.Balanza(registro);
        }
        else
        {
            registro.action = ActionType.Create;
            resultado = await _balanzaService.Balanza(registro);
            _registroId = resultado.baz_id;

            if (ImagenesCapturadas != null && ImagenesCapturadas.Any())
            {
                await _imageAuditService.GuardarImagenesLocalmenteAsync(
                    ImagenesCapturadas,
                    resultado.baz_path,
                    resultado.baz_media);
            }
        }

        Baz_des = resultado.baz_des;
        Baz_nro = resultado.baz_nro;
        _registroActual = resultado;
        TieneFotos = !string.IsNullOrEmpty(_registroActual.baz_media) || !string.IsNullOrEmpty(_registroActual.baz_media1);

        // Actualizar estado de la UI
        bool esNuevoRegistro = !EsEdicion;
        EsEdicion = true;
        PuedeEditarPlaca = false;
        TextoBotonGuardar = "Actualizar";
        PuedeImprimir = true;

        // Notificar cambios en comandos
        ImprimirCommand.NotifyCanExecuteChanged();
        GuardarCommand.NotifyCanExecuteChanged();

        await DialogService.ShowSuccess(
            esNuevoRegistro ?
                $"REGISTRO {baz_des} GUARDADO" :
                $"REGISTRO {baz_des} ACTUALIZADO", "Éxito", dialogIdentifier: DialogIdentifier);
        OnSaved?.Invoke(this, EventArgs.Empty);

    }

    /// <summary>
    /// Prepara la entidad Baz con todos los datos del formulario
    /// </summary>
    private Baz PrepararRegistroParaGuardar()
    {
        var vehiculoSel = VehiculoSeleccionado;
        var veh_id = vehiculoSel.Veh.veh_id;
        vehiculoSel.Veh.veh_id = Baz_veh_id;
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
            baz_doc = Conductor,
            baz_obs = baz_obs,
            baz_t10 = (int)baz_t10,
            baz_status = _registroActual?.baz_status, 
            baz_order = _registroActual?.baz_order??0, 
            baz_fecha = _registroActual?.baz_fecha,
            baz_data = JsonSerializer.Serialize(new Baz.BazData
            {
                nombre = NombreTransportista,
                ruc = DniRucTransportista,
                //conductor = Conductor,
                col_id = Baz_col_id,
                cliente = NumDocumentoSunat,
                //licencia = Licencia,
                phone = WhatsAppCliente,
                veh_id = veh_id,
            }).ToString(),
            veh = JsonSerializer.Serialize(vehiculoSel?.Veh).ToString(),

            files = ImagenesCapturadas.Select((ms, index) =>
            {
                var bytes = ms.ToArray();
                return (IFormFile)new SimpleFormFile(bytes, "files", $"{index + 1}.jpg");
            }).ToList()
        };
    }

    private async Task ImprimirAsync()
    {
        ///<summary>
        /// El servicio retorna por ahora solo pdf, si se requiere validacion extra, utilizar la variable type
        /// que almacena el tipo de archivo
        /// </summary>
        var (pdfBytes,type) = await _findFileService.FindFile(new
        {
            url = "/logistica/balanza",
            format = FileContentType.GetContentType(FileType.Pdf),
            action = "I",
            method = "bazPDF",
            baz_id = _registroActual.baz_id
        });

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

    private async Task MostrarImagenesAsync()
    {
        if (_registroActual == null || (string.IsNullOrEmpty(_registroActual.baz_media) && string.IsNullOrEmpty(_registroActual.baz_media1)))
        {
            await DialogService.ShowInfo("El registro no tiene capturas de cámara registradas", "Sin imágenes", dialogIdentifier: DialogIdentifier);
            return;
        }

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

    private async Task CancelarAsync()
    {
        // Limpiar el formulario
        Nuevo();
        Titulo = "Mantenimiento de Balanza";
        Subtitulo = "Agregar nuevo registro de pesaje";

        // Actualizar comandos
        GuardarCommand.NotifyCanExecuteChanged();
        ImprimirCommand.NotifyCanExecuteChanged();
    }

    private async Task Nuevo()
    {
        // Limpiar todos los campos del formulario
        _registroActual = null;
        _registroId = 0;
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
        Baz_t1m_id = 9; // Efectivo
        Baz_col_id = null;

        // Limpiar campos de texto
        Baz_des = string.Empty;
        Baz_nro = null;
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
        var window = new DestareVehiculos(new DestareVehiculosModel(DialogService, LoadingService, _balanzaSearchService)) { Owner = _window };
        var result = window.ShowDialog();

        if (result == true && window.RegistroSeleccionado != null)
        {
            Nuevo();
            CargarRegistroCompleto(window.RegistroSeleccionado);

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
        Baz.BazData? baz_data = JsonSerializer.Deserialize<Baz.BazData>(
            JsonSerializer.Serialize(baz.baz_data)
        );
        Veh? veh = JsonSerializer.Deserialize<Veh>(
            JsonSerializer.Serialize(baz.veh)
        );
        if (baz == null) return;

        // Guardar registro actual para acceder a datos adicionales (imágenes, etc.)
        _registroActual = baz;
        status = _registroActual.baz_status;

        _registroId = baz.baz_id;
        EsEdicion = true;
        PuedeImprimir = true;
        PuedeEditarPlaca = false;
        TextoBotonGuardar = "Actualizar";
        Titulo = "Editar Registro de Balanza";
        Subtitulo = $"Modificando registro {baz.baz_des}";

        // Datos básicos
        Baz_des = baz.baz_des;
        Baz_nro = baz.baz_nro;
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

        // Tipo de pago
        Baz_t1m_id = baz.baz_t1m_id;

        // Colaborador interno
        Baz_col_id = baz_data?.col_id;

        // Documentos
        Baz_doc = baz.baz_doc;
        Baz_obs = baz.baz_obs;

        // Comprobante SUNAT
        if (baz.baz_t10.HasValue)
        {
            Baz_t10 = baz.baz_t10.Value;
        }
        NombreTransportista = baz_data?.nombre;
        DniRucTransportista = baz_data?.ruc;
        WhatsAppCliente = baz_data?.phone;
        NumDocumentoSunat = baz_data?.cliente;

        TieneFotos = !string.IsNullOrEmpty(baz.baz_media) || !string.IsNullOrEmpty(baz.baz_media1);
        MostrarImagenesCommand.NotifyCanExecuteChanged();

        if (baz.veh != null && veh?.veh_id != null)
        {
            var vehiculo = Vehiculos.FirstOrDefault(v => v.Id == baz_data?.veh_id);
            if (vehiculo != null)
            {
                vehiculo.EstaSeleccionado = true;
            }
        }
        Baz_monto = baz.baz_monto;
    }


    private async Task CapturarFotosCamarasAsync()
    {
        try
        {
            if (ImagenesCapturadas != null && ImagenesCapturadas.Any())
            {
                foreach (var stream in ImagenesCapturadas)
                {
                    stream?.Dispose();
                }
                ImagenesCapturadas.Clear();
            }

            var sede = await _configurationService.GetSedeActivaAsync();
            if (sede == null) return;

            var balanzaConfig = sede.Balanzas.FirstOrDefault(b => b.Activa);
            if (balanzaConfig == null || !balanzaConfig.CanalesCamaras.Any()) return;

            ImagenesCapturadas = await _imageAuditService.CapturarImagenesAsync(balanzaConfig.Nombre);
        }
        catch (Exception ex)
        {

        }
    }
}
