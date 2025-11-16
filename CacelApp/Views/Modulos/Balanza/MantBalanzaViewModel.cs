using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Shared.Controls;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Shared.Entities;
using Infrastructure.Services.Balanza;
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
public partial class MantBalanzaViewModel : ViewModelBase
{
    private readonly IBalanzaWriteService _balanzaWriteService;
    private readonly IBalanzaReportService _balanzaReportService;
    private Window _window;
    private int _registroId;

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
    private string? nroDocumentoTransportista;

    [ObservableProperty]
    private string? docReferencia;

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
        Vehiculos.FirstOrDefault(v => v.IsSelected);

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

    public MantBalanzaViewModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IBalanzaWriteService balanzaWriteService,
        IBalanzaReportService balanzaReportService) : base(dialogService, loadingService)
    {
        _window = null!; // Se asignará después desde el code-behind
        _balanzaWriteService = balanzaWriteService ?? throw new ArgumentNullException(nameof(balanzaWriteService));
        _balanzaReportService = balanzaReportService ?? throw new ArgumentNullException(nameof(balanzaReportService));

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
                    if (ve.PropertyName == nameof(VehiculoItemViewModel.IsSelected) && vehiculo.IsSelected)
                    {
                        // Actualizar monto cuando se selecciona un vehículo
                        Monto = vehiculo.Precio;
                        
                        // Deseleccionar otros
                        foreach (var v in Vehiculos.Where(v => v != vehiculo))
                            v.IsSelected = false;

                        GuardarCommand.NotifyCanExecuteChanged();
                    }
                };
            }
        };

        // Cargar datos iniciales
        _ = CargarDatosInicialesAsync();
    }

    #region Métodos Privados

    private async Task CargarDatosInicialesAsync()
    {
        try
        {
            LoadingService.StartLoading();

            // Cargar vehículos con sus imágenes
            CargarVehiculos();

            // TODO: Cargar tipos de pago
            // await CargarTiposPagoAsync();

            // Valores por defecto
            TipoOperacion = TipoOperacionBalanza.CompraExterna;
            TipoPagoSeleccionado = 9; // Valor por defecto
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al cargar datos iniciales");
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
                IsSelected = false
            });
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
            await DialogService.ShowWarning("Debe seleccionar un vehículo", "Validación");
            return false;
        }

        // Validar placa
        if (string.IsNullOrWhiteSpace(Placa))
        {
            await DialogService.ShowWarning("Debe ingresar una placa", "Validación");
            return false;
        }

        if (Placa.Length < 6)
        {
            await DialogService.ShowWarning("La placa debe tener al menos 6 caracteres", "Validación");
            return false;
        }

        if (Placa.Length > 8)
        {
            await DialogService.ShowWarning("La placa debe tener máximo 8 caracteres", "Validación");
            return false;
        }

        // Validar tipo de operación
        if (!TipoOperacion.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un tipo de operación", "Validación");
            return false;
        }

        // Validar pesos
        if (!PesoBruto.HasValue || !PesoTara.HasValue || !PesoNeto.HasValue)
        {
            await DialogService.ShowWarning("Debe capturar el peso de la balanza", "Validación");
            return false;
        }

        // Validar tipo de pago
        if (!TipoPagoSeleccionado.HasValue)
        {
            await DialogService.ShowWarning("Debe seleccionar un tipo de pago", "Validación");
            return false;
        }

        // Validar WhatsApp si es necesario
        if (TipoPagoSeleccionado == 6 && string.IsNullOrWhiteSpace(WhatsAppCliente))
        {
            await DialogService.ShowWarning("Debe ingresar el WhatsApp del cliente", "Validación");
            return false;
        }

        // Validar documento SUNAT
        if (TipoComprobante == TipoComprobanteSunat.Boleta)
        {
            if (string.IsNullOrWhiteSpace(NumDocumentoSunat))
            {
                await DialogService.ShowWarning("Debe ingresar el DNI para boleta", "Validación");
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
                await DialogService.ShowWarning("Debe ingresar el RUC para factura", "Validación");
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
            await DialogService.ShowWarning("Debe seleccionar un colaborador interno", "Validación");
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

            if (!PesoBalanza.HasValue)
            {
                await DialogService.ShowWarning("No se pudo capturar el peso de la balanza", "Captura de Peso");
                return;
            }

            var pesoActual = PesoBalanza.Value;

            // Lógica de cálculo de pesos
            if (PesoBruto.HasValue)
            {
                if (pesoActual > PesoBruto.Value)
                {
                    // El peso actual es mayor, se actualiza como bruto
                    PesoTara = PesoBruto;
                    PesoBruto = pesoActual;
                    _pesoBrutoFijo = pesoActual;
                }
                else
                {
                    // El peso actual es menor, se toma como tara
                    PesoBruto = _pesoBrutoFijo;
                    PesoTara = pesoActual;
                }
            }
            else
            {
                // Primera captura, se asigna como bruto
                PesoBruto = pesoActual;
                _pesoBrutoFijo = pesoActual;
            }

            // Calcular peso neto
            if (PesoBruto.HasValue && PesoTara.HasValue)
            {
                PesoNeto = PesoBruto.Value - PesoTara.Value;
            }

            // TODO: Capturar fotos de cámaras
            // await CapturarFotosCamarasAsync();

            GuardarCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al capturar peso");
        }
    }

    private async Task GuardarAsync()
    {
        try
        {
            // Validar formulario
            if (!await ValidarFormularioAsync())
                return;

            // Confirmar si es edición y hay cambios críticos
            if (EsEdicion && PesoBruto == PesoNeto)
            {
                var confirmar = await DialogService.ShowConfirm(
                    "Confirmación",
                    $"Se detectó igualdad entre peso bruto y peso neto.\n\n" +
                    $"Peso Bruto: {PesoBruto:N2}\n" +
                    $"Peso Tara: {PesoTara:N2}\n" +
                    $"Peso Neto: {PesoNeto:N2}\n\n" +
                    $"¿Desea continuar con la actualización?");

                if (!confirmar)
                    return;
            }

            LoadingService.StartLoading();

            // TODO: Preparar DTO y enviar al servicio
            // var dto = PrepararDtoParaGuardar();
            // var response = await _balanzaWriteService.GuardarAsync(dto);

            // Simular respuesta
            await Task.Delay(1000);

            await DialogService.ShowSuccess(
                "Éxito",
                EsEdicion ? "Registro actualizado correctamente" : "Registro guardado correctamente");

            // Actualizar estado
            EsEdicion = true;
            PuedeEditarPlaca = false;
            TextoBotonGuardar = "Actualizar";
            PuedeImprimir = true;
            
            // Cerrar ventana con resultado exitoso
            _window.DialogResult = true;
            _window.Close();

            // TODO: Actualizar NTicket con el valor devuelto
            // NTicket = response.Data.CodigoTicket;
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
            await DialogService.ShowError(ex.Message, "Error al generar reporte");
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
            await DialogService.ShowInfo("Función de visualización de imágenes en desarrollo", "Imágenes");
        }
        catch (Exception ex)
        {
            await DialogService.ShowError(ex.Message, "Error al mostrar imágenes");
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
                "No"
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
            vehiculo.IsSelected = false;
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
        NroDocumentoTransportista = string.Empty;
        DocReferencia = string.Empty;
        
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
        
        await DialogService.ShowInfo("Formulario limpiado. Puede ingresar un nuevo registro.", "Nuevo Registro");
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
        );
    }

    #endregion

    /// <summary>
    /// Método para cargar datos de un registro existente (modo edición)
    /// </summary>
    public void CargarRegistro(BalanzaRegistroDto registro)
    {
        if (registro == null) return;

        EsEdicion = true;
        PuedeEditarPlaca = false;
        TextoBotonGuardar = "Actualizar";
        PuedeImprimir = true;
        Titulo = "Editar Registro de Balanza";
        Subtitulo = $"Modificando registro {registro.Descripcion}";

        NTicket = registro.Descripcion;
        Placa = registro.Placa;
        Cliente = registro.Referencia;

        // TODO: Seleccionar vehículo correspondiente
        // TODO: Cargar tipo de operación
        // TODO: Cargar pesos
        // TODO: Cargar demás campos

        PesoBruto = registro.PesoBruto;
        PesoTara = registro.PesoTara;
        PesoNeto = registro.PesoNeto;
        _pesoBrutoFijo = registro.PesoBruto ?? 0;
        Monto = registro.Monto;
        Observaciones = registro.Observaciones;

        // TODO: Cargar más campos del DTO
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
    private bool isSelected;

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
