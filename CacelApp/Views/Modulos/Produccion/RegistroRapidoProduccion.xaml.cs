using System;
using System.Windows;
using System.Windows.Input;

using KeyEventArgs = global::System.Windows.Input.KeyEventArgs;
using Key = global::System.Windows.Input.Key;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// Lógica de interacción para RegistroRapidoProduccion.xaml
/// </summary>
public partial class RegistroRapidoProduccion : Window
{
    private readonly RegistroRapidoProduccionModel _viewModel;
    private bool _isInitializing = true;

    public RegistroRapidoProduccion(RegistroRapidoProduccionModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Manejar tecla Enter global
        KeyDown += Window_KeyDown;
        Closed += Window_Closed;
        
        // Foco inicial en Visor (Paso 1)
        Loaded += async (s, e) => {
             await global::System.Threading.Tasks.Task.Delay(500); // Un poco más de tiempo para asentar datos
             VisorControl.Focus();
             _viewModel.CurrentStep = 1;
             _isInitializing = false; // Fin de inicialización
        };

        // EVENTOS DE FOCO PARA ACTUALIZAR PASOS (Click o Tab manual)
        GroupMaterial.GotFocus += (s, e) => _viewModel.CurrentStep = 2;
        GroupUnidad.GotFocus += (s, e) => _viewModel.CurrentStep = 3;
        GroupMaquinaria.GotFocus += (s, e) => _viewModel.CurrentStep = 4;
        TxtTara.GotFocus += (s, e) => _viewModel.CurrentStep = 5;

        // Suscribirse al cambio de valor para avanzar al paso 3
        _viewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(RegistroRapidoProduccionModel.MaterialSeleccionado))
            {
                if (_isInitializing || _viewModel.MaterialSeleccionado == null) return;
                
                Dispatcher.InvokeAsync(() => {
                    _viewModel.CurrentStep = 3;
                    GroupUnidad.Focus();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        };
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F3)
        {
            _viewModel.CurrentStep = 2;
            GroupMaterial.FocusSearch();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            // PRIORIDAD GLOBAL: Si el flujo está completo (tenemos Neto y datos), Enter = GUARDAR
            // Esto permite que si el usuario vuelve al Visor (Paso 1) a re-pesar, el Enter guarde directamente.
            if (_viewModel.PesoNeto > 0 && 
                _viewModel.MaterialSeleccionado.HasValue && 
                !string.IsNullOrEmpty(_viewModel.Pes_veh_id) &&
                _viewModel.GuardarCommand.CanExecute(null))
            {
                _viewModel.GuardarCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Paso 1: Capturar Peso (Flujo inicial o sin datos completos)
            if (_viewModel.CurrentStep == 1)
            {
                // Si ya capturó algo (> 0) pero aún no tenemos el flujo completo (por eso llegamos aquí)
                if (_viewModel.PesoBruto > 0)
                {
                    _viewModel.CurrentStep = 2;
                    Dispatcher.InvokeAsync(() => GroupMaterial.FocusSearch(), System.Windows.Threading.DispatcherPriority.Background);
                }
                else if (_viewModel.PrimeraBalanza?.CapturarCommand?.CanExecute(null) == true)
                {
                    _viewModel.PrimeraBalanza.CapturarCommand.Execute(null);
                    _viewModel.CurrentStep = 2;
                    Dispatcher.InvokeAsync(() => GroupMaterial.FocusSearch(), System.Windows.Threading.DispatcherPriority.Background);
                }
                e.Handled = true;
            }
            // Fallback para otros pasos si no se cumplió la condición global de arriba
            else if (_viewModel.CurrentStep >= 4 && _viewModel.GuardarCommand.CanExecute(null))
            {
                _viewModel.GuardarCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _viewModel.Cleanup();
    }

    // NAVEGACIÓN AUTOMÁTICA AL SELECCIONAR

    private void Material_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        // Al seleccionar material (Paso 2), pasar foco a Unidad (Paso 3) de forma diferida
        // para dar tiempo a que el DialogHost cierre y restaure foco
        Dispatcher.InvokeAsync(() => {
            _viewModel.CurrentStep = 3;
            GroupUnidad.Focus();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void UnidadMedida_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        // Al seleccionar medida (Paso 3), pasar foco a Maquinaria (Paso 4)
        Dispatcher.InvokeAsync(() => {
            _viewModel.CurrentStep = 4;
            GroupMaquinaria.Focus();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void Maquinaria_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        // Al seleccionar maquinaria (Paso 4), pasar foco a Tara (Paso 5)
        Dispatcher.InvokeAsync(() => {
            _viewModel.CurrentStep = 5;
            TxtTara.Focus();
            TxtTara.SelectAll(); // Facilitar edición del peso manual
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void TxtTara_GotFocus(object sender, RoutedEventArgs e)
    {
        _viewModel.CurrentStep = 5;
    }

    private void LoadingOverlay_Loaded(object sender, RoutedEventArgs e)
    {
    }
}
