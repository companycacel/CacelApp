using CacelApp.Services.Dialog;
using CacelApp.Services.Image;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls.ImageViewer;
using CacelApp.Shared.Controls.PdfViewer;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Balanza.Entities;
using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Core.Shared.Enums;
using Infrastructure.Services.Balanza;
using Infrastructure.Services.Shared;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CacelApp.Views.Modulos.Balanza;

/// <summary>
/// ViewModel para la ventana de mantenimiento de registros de Balanza
/// Implementa todas las validaciones y lógica de negocio del formulario
/// </summary>
public partial class MantBalanzaModel : ViewModelBase
{
    private readonly IBalanzaWriteService _balanzaWriteService;
    private readonly IBalanzaReportService _balanzaReportService;
    private readonly ISelectOptionService _selectOptionService;
    private readonly IImageLoaderService _imageLoaderService;
    private Window _window;
    private int _registroId;
    private Baz? _registroActual; // Guardar el registro completo para acceder a datos adicionales
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
    private string? nTicket;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardar))]
    [NotifyCanExecuteChangedFor(nameof(GuardarCommand))]
    private string? placa;

    [ObservableProperty]
    private string? cliente;

    [ObservableProperty]
    private bool puedeEditarPlaca = true;

    // Vehículos
    [ObservableProperty]
    private ObservableCollection<VehiculoItemViewModel> vehiculos = new();

    // Tipo de Operación
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardar))]
    private TipoOperacionBalanza? tipoOperacion;

    // Pesos
    [ObservableProperty]
    private string nombreBalanza = "BALANZA";

    [ObservableProperty]
    private decimal? pesoBalanza;

    [ObservableProperty]
    private decimal? pesoBruto;

    [ObservableProperty]
    private decimal? pesoTara;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeGuardar))]
    private decimal? pesoNeto;

    private decimal _pesoBrutoFijo = 0;

    // Tipo de Pago
    [ObservableProperty]
    private ObservableCollection<SelectOption> tiposPago = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarColaboradorInterno), nameof(MostrarConductor), nameof(PuedeGuardar))]
    private int? tipoPagoSeleccionado;

    partial void OnTipoPagoSeleccionadoChanged(int? value)
    {
        // Cargar colaboradores cuando se selecciona tipo de pago interno (23)
        if (value == 23)
        {
            _ = CargarColaboradoresInternosAsync();
        }
        else
        {
            ColaboradoresInternos.Clear();
            ColaboradorInternoSeleccionado = null;
        }
    }

    [ObservableProperty]
    private decimal monto;

    // Colaborador Interno
    public bool MostrarColaboradorInterno => TipoPagoSeleccionado == 23;

    [ObservableProperty]
    private ObservableCollection<SelectOption> colaboradoresInternos = new();

    [ObservableProperty]
    private int? colaboradorInternoSeleccionado;

    // Conductor
    public bool MostrarConductor => TipoPagoSeleccionado != 23;

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
    private string? docReferencia;

    [ObservableProperty]
    private string? guia;

    // Comprobante SUNAT
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiereDocumentoSunat), nameof(PuedeGuardar))]
    private TipoComprobanteSunat tipoComprobante = TipoComprobanteSunat.NA;

    public bool RequiereDocumentoSunat => TipoComprobante != TipoComprobanteSunat.NA;

    [ObservableProperty]
    private string? numDocumentoSunat;

    [ObservableProperty]
    private string? observaciones;

    // Estado
    [ObservableProperty]
    private bool esEdicion;

    [ObservableProperty]
    private bool estadoCamara;

    [ObservableProperty]
    private bool tieneFotos;

    [ObservableProperty]
    private bool puedeImprimir;

    [ObservableProperty]
    private string textoBotonGuardar = "Guardar";

    public bool PuedeGuardar =>
        !string.IsNullOrWhiteSpace(Placa) &&
        VehiculoSeleccionado != null &&
        TipoOperacion.HasValue &&
        PesoNeto.HasValue &&
        TipoPagoSeleccionado.HasValue &&
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
        IBalanzaWriteService balanzaWriteService,
        IBalanzaReportService balanzaReportService,
        ISelectOptionService selectOptionService,
        IImageLoaderService imageLoaderService) : base(dialogService, loadingService)
    {
        _window = null!; // Se asignará después desde el code-behind
        _balanzaWriteService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));
        _imageLoaderService = imageLoaderService ?? throw new ArgumentNullException(nameof(imageLoaderService));

        // Inicializar comandos
        CapturarPesoCommand = new AsyncRelayCommand(CapturarPesoAsync);
        GuardarCommand = new AsyncRelayCommand(GuardarAsync, () => PuedeGuardar);
        ImprimirCommand = new AsyncRelayCommand(ImprimirAsync, () => PuedeImprimir);
        MostrarImagenesCommand = new AsyncRelayCommand(MostrarImagenesAsync, () => TieneFotos);
        NuevoCommand = new AsyncRelayCommand(Nuevo);
        DestareCommand = new AsyncRelayCommand(DestareAsync);
        CancelarCommand = new AsyncRelayCommand(CancelarAsync);
        CerrarCommand = new RelayCommand(() => _window.Close());

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
                            // Actualizar monto cuando se selecciona un vehículo
                            Monto = vehiculo.Precio;
                            
                            // Deseleccionar otros
                            foreach (var v in Vehiculos.Where(v => v != vehiculo))
                                v.EstaSeleccionado = false;
                        }
                        
                        // Siempre notificar que cambió la condición de guardado
                        GuardarCommand.NotifyCanExecuteChanged();
                    }
                };
            }
        };

        // No cargar datos aquí, se cargarán desde el evento Loaded de la ventana
    }

    #region Métodos Privados

    public async Task CargarDatosInicialesAsync()
    {
        try
        {
            LoadingService.StartLoading();

            // Cargar vehículos con sus imágenes
            CargarVehiculos();

            // Cargar tipos de pago
            await CargarTiposPagoAsync();

            // Valores por defecto solo para nuevo registro
            if (!EsEdicion)
            {
                TipoOperacion = TipoOperacionBalanza.CompraExterna;  // Valor 0
                TipoPagoSeleccionado = 9; // Contado por defecto
            }
            
            await Task.CompletedTask;
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

        // Cargar vehículos con sus imágenes y precios
        // Las imágenes se cargan desde Assets/Image/trucks
        // veh_neje es el número de ejes (ID en BD)
        // veh_ref es el precio
        // veh_year es la capacidad en toneladas
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
            Vehiculos.Add(new VehiculoItemViewModel
            {
                Id = vehiculo.Neje,  // Usar Neje como ID (número de ejes)
                Nombre = vehiculo.Nombre,
                Precio = vehiculo.Precio,
                Capacidad = vehiculo.Capacidad,
                ImagenUrl = $"pack://application:,,,/Assets/Image/trucks/{vehiculo.Imagen}",
                EstaSeleccionado = false
            });
        }
    }

    private async Task CargarTiposPagoAsync()
    {
        try
        {
            // Limpiar tipos de pago existentes
            TiposPago.Clear();

            // Cargar desde servicio
            var tiposPago = await _selectOptionService.GetSelectOptionsAsync(SelectOptionType.TipoPago);
            
            foreach (var tipo in tiposPago)
            {
                TiposPago.Add(tipo);
            }

            // Si no se obtienen datos del servicio, cargar valores por defecto
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
        if (TipoPagoSeleccionado == 6 && string.IsNullOrWhiteSpace(WhatsAppCliente))
            return false;

        // Si requiere documento SUNAT, validar
        if (RequiereDocumentoSunat && string.IsNullOrWhiteSpace(NumDocumentoSunat))
            return false;

        // Si es colaborador interno, validar selección
        if (MostrarColaboradorInterno && !ColaboradorInternoSeleccionado.HasValue)
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
        if (string.IsNullOrWhiteSpace(Placa))
        {
            await DialogService.ShowWarning("Debe ingresar una placa", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        if (Placa.Length < 6)
        {
            await DialogService.ShowWarning("La placa debe tener al menos 6 caracteres", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        if (Placa.Length > 8)
        {
            await DialogService.ShowWarning("La placa debe tener máximo 8 caracteres", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar tipo de operación
        if (!TipoOperacion.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un tipo de operación", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar pesos
        if (!PesoBruto.HasValue || !PesoTara.HasValue || !PesoNeto.HasValue)
        {
            await DialogService.ShowWarning("Debe capturar el peso de la balanza", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar tipo de pago
        if (!TipoPagoSeleccionado.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un tipo de pago", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar WhatsApp si es necesario
        if (TipoPagoSeleccionado == 6 && string.IsNullOrWhiteSpace(WhatsAppCliente))
        {
            await DialogService.ShowWarning("Debe ingresar el WhatsApp del cliente", "Validación", dialogIdentifier: DialogIdentifier);
            return false;
        }

        // Validar documento SUNAT
        if (TipoComprobante == TipoComprobanteSunat.Boleta)
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
        else if (TipoComprobante == TipoComprobanteSunat.Factura)
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
        if (MostrarColaboradorInterno && !ColaboradorInternoSeleccionado.HasValue)
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
            // TODO: Implementar captura de peso desde balanza serial
            // PesoBalanza = await CapturarPesoDesdeBalanzaAsync();

            // Simular lectura de peso (en producción esto vendría del puerto serial)
            if (!PesoBalanza.HasValue || PesoBalanza == 0)
            {
                await DialogService.ShowWarning("No se ha capturado el peso de la balanza.\nAsegúrese de que la balanza esté conectada y transmitiendo.", "Captura de Peso", dialogIdentifier: DialogIdentifier);
                return;
            }

            var pesoActual = PesoBalanza.Value;

            // Lógica de cálculo de pesos según si es primera captura o segunda (destare)
            if (!EsEdicion)
            {
                // Primera captura - modo CREATE
                PesoBruto = pesoActual;
                PesoTara = 0;
                _pesoBrutoFijo = pesoActual;
                // Status = 1 (pesado una vez)
            }
            else
            {
                // Segunda captura - modo UPDATE (destare)
                _pesoBrutoFijo = PesoBruto.Value; // Guardar el peso bruto original

                if (pesoActual > _pesoBrutoFijo)
                {
                    // El peso actual es mayor que el bruto anterior
                    // El bruto anterior se convierte en tara
                    PesoTara = _pesoBrutoFijo;
                    PesoBruto = pesoActual;
                    _pesoBrutoFijo = pesoActual;
                    // baz_order = 1 (bruto después)
                }
                else
                {
                    // El peso actual es menor que el bruto anterior
                    // El peso actual es la tara
                    PesoBruto = _pesoBrutoFijo;
                    PesoTara = pesoActual;
                    // baz_order = 0 (bruto primero)
                }
                // Status = 2 (pesado dos veces, completo)
            }

            // Calcular peso neto
            PesoNeto = PesoBruto.Value - (PesoTara ?? 0);

            // TODO: Capturar fotos de cámaras
            // await CapturarFotosCamarasAsync();
            // TieneFotos = true;

            await DialogService.ShowSuccess("Peso capturado correctamente", "Captura de Peso", dialogIdentifier: DialogIdentifier);

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
                if (PesoBruto == PesoNeto)
                {
                    alertas += "⚠️ Se detectó igualdad entre peso bruto y peso neto (tara en 0)\n";
                }

                if (!string.IsNullOrEmpty(alertas))
                {
                    var confirmar = await DialogService.ShowConfirm(
                        "Confirmación de Actualización",
                        $"¿Está seguro de actualizar el registro N° {NTicket}?\n\n" +
                        $"Peso Bruto: {PesoBruto:N2} kg\n" +
                        $"Peso Tara: {PesoTara:N2} kg\n" +
                        $"Peso Neto: {PesoNeto:N2} kg\n\n" +
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
                registro.baz_id = _registroId;
                resultado = await _balanzaWriteService.ActualizarRegistroAsync(registro);
            }
            else
            {
                resultado = await _balanzaWriteService.CrearRegistroAsync(registro);
                _registroId = resultado.baz_id;
            }

            // Actualizar NTicket con el valor devuelto
            NTicket = resultado.baz_des;

            // Actualizar estado de la UI
            EsEdicion = true;
            PuedeEditarPlaca = false;
            TextoBotonGuardar = "Actualizar";
            PuedeImprimir = true;

            await DialogService.ShowSuccess(
                "Éxito",
                EsEdicion ? 
                    $"Registro {NTicket} actualizado correctamente" : 
                    $"Registro {NTicket} guardado correctamente", dialogIdentifier: DialogIdentifier);

            // Cerrar ventana con resultado exitoso
            _window.DialogResult = true;
            _window.Close();
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
            baz_veh_id = Placa?.ToUpper() ?? string.Empty,
            baz_ref = Cliente,
            baz_tipo = (int?)TipoOperacion,  // 0, 1 o 2
            baz_pb = PesoBruto,
            baz_pt = PesoTara,
            baz_pn = PesoNeto,
            baz_t1m_id = TipoPagoSeleccionado,
            baz_monto = Monto,
            baz_col_id = ColaboradorInternoSeleccionado,
            baz_doc = DocReferencia,
            baz_obs = Observaciones,
            baz_t10 = (int)TipoComprobante,
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
            files = null // Se agregará cuando se implemente captura de cámaras
        };
    }

    private async Task ImprimirAsync()
    {
        try
        {
            LoadingService.StartLoading();

            // TODO: Obtener PDF del servicio
            // var pdfBytes = await _balanzaReportService.GenerarReporteAsync(registroId);

            // Simular PDF
            await Task.Delay(500);
            byte[] pdfBytes = Array.Empty<byte>();

            if (pdfBytes.Length > 0)
            {
                var pdfWindow = new PdfViewerWindow(pdfBytes, $"Reporte {NTicket}");
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
                $"Registro: {NTicket} - Placa: {Placa}");

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
        if (EsEdicion || !string.IsNullOrEmpty(NTicket) || PesoBruto > 0)
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
    }

    private async Task Nuevo()
    {
        // Limpiar todos los campos del formulario
        _registroId = 0;
        PesoBalanza = 0;
        PesoBruto = 0;
        PesoTara = 0;
        PesoNeto = 0;
        _pesoBrutoFijo = 0;
        
        // Limpiar selecciones de vehículos
        foreach (var vehiculo in Vehiculos)
        {
            vehiculo.EstaSeleccionado = false;
        }
        
        TipoPagoSeleccionado = null;
        ColaboradorInternoSeleccionado = null;
        
        // Limpiar campos de texto
        NTicket = string.Empty;
        Placa = string.Empty;
        Cliente = string.Empty;
        Observaciones = string.Empty;
        WhatsAppCliente = string.Empty;
        NumDocumentoSunat = string.Empty;
        Conductor = string.Empty;
        Licencia = string.Empty;
        NombreTransportista = string.Empty;
        DniRucTransportista = string.Empty;
        DocReferencia = string.Empty;
        Guia = string.Empty;
        
        // Resetear tipo de operación y comprobante
        TipoOperacion = TipoOperacionBalanza.CompraExterna;
        TipoComprobante = TipoComprobanteSunat.NA;
        
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
        
        await DialogService.ShowInfo("Formulario limpiado. Puede ingresar un nuevo registro.", "Nuevo Registro", dialogIdentifier: DialogIdentifier);
    }

    private async Task DestareAsync()
    {
        // TODO: Implementar búsqueda de registro existente para destare
        // Este método debe:
        // 1. Mostrar un diálogo de búsqueda de registros previos
        // 2. Permitir filtrar por vehículo, fecha, referencia, etc.
        // 3. Al seleccionar un registro, cargar sus datos
        // 4. Marcar el modo como "Destare" para usar el peso del registro previo como tara
        
        await DialogService.ShowInfo(
            "Funcionalidad en desarrollo.\n\n" +
            "Este botón permitirá buscar un registro existente para usar su peso como tara del nuevo registro.\n\n" +
            "Pasos típicos:\n" +
            "1. Buscar registro anterior por placa o ticket\n" +
            "2. Cargar datos básicos del registro\n" +
            "3. Usar PesoNeto del registro anterior como PesoTara del nuevo\n" +
            "4. Capturar nuevo peso bruto para calcular peso neto final",
            "Destare"
        , dialogIdentifier: DialogIdentifier);
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
        PuedeEditarPlaca = false;
        TextoBotonGuardar = "Actualizar";
        PuedeImprimir = true;
        Titulo = "Editar Registro de Balanza";
        Subtitulo = $"Modificando registro {baz.baz_des}";

        // Datos básicos
        NTicket = baz.baz_des;
        Placa = baz.baz_veh_id;
        Cliente = baz.baz_ref;

        // Tipo de operación
        if (baz.baz_tipo.HasValue)
        {
            TipoOperacion = (TipoOperacionBalanza)baz.baz_tipo.Value;
        }

        // Pesos
        PesoBruto = baz.baz_pb;
        PesoTara = baz.baz_pt;
        PesoNeto = baz.baz_pn;
        _pesoBrutoFijo = baz.baz_pb ?? 0;

        // Tipo de pago y monto
        TipoPagoSeleccionado = baz.baz_t1m_id;
        Monto = baz.baz_monto;

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
        ColaboradorInternoSeleccionado = baz.baz_col_id;

        // Documentos
        DocReferencia = baz.baz_doc;
        Observaciones = baz.baz_obs;

        // Comprobante SUNAT
        if (baz.baz_t10.HasValue)
        {
            TipoComprobante = (TipoComprobanteSunat)baz.baz_t10.Value;
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

}

/// <summary>
/// ViewModel para items de vehículos en la selección
/// </summary>
public partial class VehiculoItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int id;  // veh_neje (número de ejes)

    [ObservableProperty]
    private string nombre = string.Empty;

    [ObservableProperty]
    private decimal precio;  // veh_ref

    [ObservableProperty]
    private string capacidad = string.Empty;  // veh_year

    [ObservableProperty]
    private bool estaSeleccionado;

    [ObservableProperty]
    private string imagenUrl = string.Empty;
}

/// <summary>
/// Enumeración para tipos de operación en balanza
/// Valores deben coincidir con baz_tipo en la base de datos
/// </summary>
public enum TipoOperacionBalanza
{
    CompraExterna = 0,   // rbOpce.Tag = "0" en WinForms
    IngresoDev = 1,      // rbOpid.Tag = "1" en WinForms (Ingreso Despacho)
    IngresoRep = 2       // rbOpir.Tag = "2" en WinForms (Ingreso Recepción)
}

/// <summary>
/// Enumeración para tipos de comprobante SUNAT
/// </summary>
public enum TipoComprobanteSunat
{
    NA = 0,
    Boleta = 1,
    Factura = 2
}

