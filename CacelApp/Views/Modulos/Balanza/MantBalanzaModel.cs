using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls;
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
    private Window _window;
    private int _registroId;
    private const string DialogIdentifier = "MantBalanzaDialogHost";
    private BalanzaRegistroDto? _registroPendiente;

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
        ISelectOptionService selectOptionService) : base(dialogService, loadingService)
    {
        _window = null!; // Se asignará después desde el code-behind
        _balanzaWriteService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));
        _selectOptionService = selectOptionService ?? throw new ArgumentNullException(nameof(selectOptionService));

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
                    if (ve.PropertyName == nameof(VehiculoItemViewModel.EstaSeleccionado) && vehiculo.EstaSeleccionado)
                    {
                        // Actualizar monto cuando se selecciona un vehículo
                        Monto = vehiculo.Precio;
                        
                        // Deseleccionar otros
                        foreach (var v in Vehiculos.Where(v => v != vehiculo))
                            v.EstaSeleccionado = false;

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

            // Valores por defecto
            TipoOperacion = TipoOperacionBalanza.CompraExterna;
            TipoPagoSeleccionado = 9; // Valor por defecto
            
            // Aplicar registro pendiente si existe
            if (_registroPendiente != null)
            {
                AplicarRegistroCargado(_registroPendiente);
                _registroPendiente = null;
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
        var vehiculosData = new[]
        {
            new { Id = 1, Nombre = "1 a 3 TN", Precio = 5m, Imagen = "truck_1.png" },
            new { Id = 2, Nombre = "1 a 6 TN", Precio = 7m, Imagen = "truck_2.jpg" },
            new { Id = 3, Nombre = "1 a 10 TN", Precio = 8m, Imagen = "truck_3.png" },
            new { Id = 4, Nombre = "1 a 12 TN", Precio = 10m, Imagen = "truck_4.png" },
            new { Id = 5, Nombre = "1 a 15 TN", Precio = 12m, Imagen = "truck_5.png" },
            new { Id = 6, Nombre = "1 a 20 TN", Precio = 15m, Imagen = "truck_6.png" },
            new { Id = 7, Nombre = "1 a 30 TN", Precio = 20m, Imagen = "truck_7.png" },
            new { Id = 8, Nombre = "1 a 40 TN", Precio = 25m, Imagen = "truck_8.png" }
        };

        foreach (var vehiculo in vehiculosData)
        {
            Vehiculos.Add(new VehiculoItemViewModel
            {
                Id = vehiculo.Id,
                Nombre = vehiculo.Nombre,
                Precio = vehiculo.Precio,
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
                    "No se pudieron cargar los tipos de pago desde el servidor. Se cargarán valores por defecto.",
                    "Advertencia"
                , dialogIdentifier: DialogIdentifier);

                var tiposPagoData = new[]
                {
                    new { Value = 1, Label = "Efectivo" },
                    new { Value = 2, Label = "Tarjeta" },
                    new { Value = 3, Label = "Transferencia" },
                    new { Value = 4, Label = "Cheque" },
                    new { Value = 5, Label = "Yape/Plin" },
                    new { Value = 6, Label = "Crédito" },
                    new { Value = 7, Label = "Depósito" },
                    new { Value = 8, Label = "Letra" },
                    new { Value = 9, Label = "Contado" },
                    new { Value = 10, Label = "Vale" },
                    new { Value = 22, Label = "Interno Despacho" },
                    new { Value = 23, Label = "Interno Colaborador" }
                };

                foreach (var tipo in tiposPagoData)
                {
                    TiposPago.Add(new SelectOption
                    {
                        Value = tipo.Value,
                        Label = tipo.Label
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(
                $"Error al cargar tipos de pago: {ex.Message}",
                "Error"
            , dialogIdentifier: DialogIdentifier);

            // Cargar valores por defecto en caso de error
            var tiposPagoData = new[]
            {
                new { Value = 1, Label = "Efectivo" },
                new { Value = 2, Label = "Tarjeta" },
                new { Value = 3, Label = "Transferencia" },
                new { Value = 4, Label = "Cheque" },
                new { Value = 5, Label = "Yape/Plin" },
                new { Value = 6, Label = "Crédito" },
                new { Value = 7, Label = "Depósito" },
                new { Value = 8, Label = "Letra" },
                new { Value = 9, Label = "Contado" },
                new { Value = 10, Label = "Vale" },
                new { Value = 22, Label = "Interno Despacho" },
                new { Value = 23, Label = "Interno Colaborador" }
            };

            foreach (var tipo in tiposPagoData)
            {
                TiposPago.Add(new SelectOption
                {
                    Value = tipo.Value,
                    Label = tipo.Label
                });
            }
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
            baz_tipo = (int?)TipoOperacion,
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
            // TODO: Implementar visualización de imágenes capturadas
            await DialogService.ShowInfo("Función de visualización de imágenes en desarrollo", "Imágenes", dialogIdentifier: DialogIdentifier);
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al mostrar imágenes", dialogIdentifier: DialogIdentifier);
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
    /// Método para cargar datos de un registro existente (modo edición)
    /// </summary>
    public void CargarRegistro(BalanzaRegistroDto registro)
    {
        if (registro == null) return;

        // Guardar el registro para cargarlo después de inicializar los datos
        _registroPendiente = registro;

        EsEdicion = true;
        PuedeEditarPlaca = false;
        TextoBotonGuardar = "Actualizar";
        PuedeImprimir = true;
        Titulo = "Editar Registro de Balanza";
        Subtitulo = $"Modificando registro {registro.Descripcion}";

        NTicket = registro.Descripcion;
        Placa = registro.Placa;
        Cliente = registro.Referencia;
    }

    private void AplicarRegistroCargado(BalanzaRegistroDto registro)
    {
        // Seleccionar vehículo correspondiente por el monto (precio)
        if (registro.Monto > 0)
        {
            var vehiculo = Vehiculos.FirstOrDefault(v => v.Precio == registro.Monto);
            if (vehiculo != null)
            {
                vehiculo.EstaSeleccionado = true;
            }
        }

        // Cargar tipo de operación
        if (registro.Tipo.HasValue)
        {
            TipoOperacion = (TipoOperacionBalanza)registro.Tipo.Value;
        }

        // Cargar pesos
        PesoBruto = registro.PesoBruto;
        PesoTara = registro.PesoTara;
        PesoNeto = registro.PesoNeto;
        _pesoBrutoFijo = registro.PesoBruto ?? 0;
        
        Monto = registro.Monto;
        Observaciones = registro.Observaciones;
        NumDocumentoSunat = registro.Documento;
    }
}

/// <summary>
/// ViewModel para items de vehículos en la selección
/// </summary>
public partial class VehiculoItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string nombre = string.Empty;

    [ObservableProperty]
    private decimal precio;

    [ObservableProperty]
    private bool estaSeleccionado;

    [ObservableProperty]
    private string imagenUrl = string.Empty;
}

/// <summary>
/// Enumeración para tipos de operación en balanza
/// </summary>
public enum TipoOperacionBalanza
{
    CompraExterna = 1,
    IngresoDev = 2,
    IngresoRep = 3
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

