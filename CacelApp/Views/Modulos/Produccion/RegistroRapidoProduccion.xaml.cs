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

        KeyDown += Window_KeyDown;
        Closed += Window_Closed;
        
        Loaded += async (s, e) => {
             await global::System.Threading.Tasks.Task.Delay(500); 
             VisorControl.Focus();
             _viewModel.CurrentStep = 1;
             _isInitializing = false; 
        };

        GroupMaterial.GotFocus += (s, e) => {
            _viewModel.CurrentStep = 2;
            Dispatcher.InvokeAsync(() => GroupMaterial.FocusSearch(), System.Windows.Threading.DispatcherPriority.Input);
        };
        GroupUnidad.GotFocus += (s, e) => _viewModel.CurrentStep = 3;
        GroupMaquinaria.GotFocus += (s, e) => _viewModel.CurrentStep = 4;
        TxtTara.GotFocus += (s, e) => _viewModel.CurrentStep = 5;

        _viewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(RegistroRapidoProduccionModel.MaterialSeleccionado))
            {
                if (_isInitializing || _viewModel.MaterialSeleccionado == null) return;
                
                Dispatcher.InvokeAsync(() => {
                    _viewModel.CurrentStep = 3;
                    GroupUnidad.Focus();
                }, System.Windows.Threading.DispatcherPriority.Input);
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
            if (_viewModel.PesoNeto > 0 && 
                _viewModel.MaterialSeleccionado.HasValue && 
                !string.IsNullOrEmpty(_viewModel.Pes_veh_id) &&
                _viewModel.GuardarCommand.CanExecute(null))
            {
                _viewModel.GuardarCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (_viewModel.CurrentStep == 1)
            {
                if (_viewModel.PesoBruto > 0)
                {
                    _viewModel.CurrentStep = 2;
                    Dispatcher.InvokeAsync(() => GroupMaterial.FocusSearch(), System.Windows.Threading.DispatcherPriority.Input);
                }
                else if (_viewModel.PrimeraBalanza?.CapturarCommand?.CanExecute(null) == true)
                {
                    _viewModel.PrimeraBalanza.CapturarCommand.Execute(null);
                    _viewModel.CurrentStep = 2;
                    Dispatcher.InvokeAsync(() => GroupMaterial.FocusSearch(), System.Windows.Threading.DispatcherPriority.Input);
                }
                e.Handled = true;
            }
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
        Dispatcher.InvokeAsync(() => {
            _viewModel.CurrentStep = 3;
            GroupUnidad.Focus();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void UnidadMedida_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        Dispatcher.InvokeAsync(() => {
            _viewModel.CurrentStep = 4;
            GroupMaquinaria.Focus();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void Maquinaria_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        Dispatcher.InvokeAsync(() => {
            _viewModel.CurrentStep = 5;
            TxtTara.Focus();
            TxtTara.SelectAll(); 
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
