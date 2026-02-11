using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;

namespace CacelApp.Views.Modulos.Produccion
{
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

            PreviewKeyDown += Window_PreviewKeyDown;
            KeyDown += Window_KeyDown;
            Closed += Window_Closed;
            
            Loaded += async (s, e) => {
                 await System.Threading.Tasks.Task.Delay(500); 
                 if (VisorControl != null) VisorControl.Focus();
                 _viewModel.CurrentStep = 1;
                 _isInitializing = false; 
            };

            _viewModel.PropertyChanged += (s, ev) => {
                if (ev.PropertyName == nameof(_viewModel.CurrentStep))
                {
                    if (_viewModel.CurrentStep == 2)
                    {
                        Dispatcher.InvokeAsync(() => {
                            MaterialFilterBox.Focus();
                            MaterialFilterBox.SelectAll();
                        }, DispatcherPriority.Input);
                    }
                }
            };
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _viewModel.Cleanup();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                _viewModel.CurrentStep = 2;
                MaterialFilterBox.Focus();
                e.Handled = true;
                return;
            }

            // Navegación con TAB
            if (e.Key == Key.Tab)
            {
                if (_viewModel.CurrentStep == 1 && _viewModel.PesoBruto > 0)
                {
                    _viewModel.CurrentStep = 2;
                    Dispatcher.InvokeAsync(() => MaterialFilterBox.Focus(), DispatcherPriority.Input);
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 2 && _viewModel.MaterialSeleccionado.HasValue)
                {
                    // Ir a Unidad (O Maquinaria si 49)
                    if (_viewModel.UnidadMedidaSeleccionada == 49)
                    {
                        _viewModel.CurrentStep = 4;
                        Dispatcher.InvokeAsync(() => GroupMaquinaria.Focus(), DispatcherPriority.Input);
                    }
                    else
                    {
                        _viewModel.CurrentStep = 3;
                        Dispatcher.InvokeAsync(() => GroupUnidad.Focus(), DispatcherPriority.Input);
                    }
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 3 && _viewModel.UnidadMedidaSeleccionada.HasValue)
                {
                    // Ir a Maquinaria
                    _viewModel.CurrentStep = 4;
                    Dispatcher.InvokeAsync(() => GroupMaquinaria.Focus(), DispatcherPriority.Input);
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 4)
                {
                    // Ir a Tara (Paso 5)
                    _viewModel.CurrentStep = 5;
                    Dispatcher.InvokeAsync(() =>
                    {
                        TxtTara.Focus();
                        TxtTara.SelectAll();
                    }, DispatcherPriority.Input);
                    e.Handled = true;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Si se puede guardar, Guardar (Sin restricción de paso)
                if (_viewModel.CanSave)
                {
                    _viewModel.GuardarCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // Si NO se puede guardar, comportamiento de navegación (Enter = Avanzar)
                if (_viewModel.CurrentStep == 1)
                {
                    if (_viewModel.PesoBruto > 0)
                    {
                        _viewModel.CurrentStep = 2;
                        Dispatcher.InvokeAsync(() => MaterialFilterBox.Focus(), DispatcherPriority.Input);
                    }
                    else if (_viewModel.PrimeraBalanza?.CapturarCommand.CanExecute(null) == true)
                    {
                        _viewModel.PrimeraBalanza.CapturarCommand.Execute(null);
                    }
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 2 && _viewModel.MaterialSeleccionado.HasValue)
                {
                     if (_viewModel.UnidadMedidaSeleccionada == 49)
                    {
                        _viewModel.CurrentStep = 4;
                        Dispatcher.InvokeAsync(() => GroupMaquinaria.Focus(), DispatcherPriority.Input);
                    }
                    else
                    {
                        _viewModel.CurrentStep = 3;
                        Dispatcher.InvokeAsync(() => GroupUnidad.Focus(), DispatcherPriority.Input);
                    }
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 3 && _viewModel.UnidadMedidaSeleccionada.HasValue)
                {
                    _viewModel.CurrentStep = 4;
                    Dispatcher.InvokeAsync(() => GroupMaquinaria.Focus(), DispatcherPriority.Input);
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 4)
                {
                    _viewModel.CurrentStep = 5;
                    Dispatcher.InvokeAsync(() => { TxtTara.Focus(); TxtTara.SelectAll(); }, DispatcherPriority.Input);
                    e.Handled = true;
                }
            }
        }

        private void MaterialFilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Solo lógica de selección automática, NO navegación (la navegación la maneja Window_KeyDown/Tab)
            if (e.Key == Key.Enter && _viewModel.MaterialesFiltrados.Count > 0)
            {
                if (!_viewModel.MaterialSeleccionado.HasValue)
                {
                     _viewModel.MaterialSeleccionado = (int?)_viewModel.MaterialesFiltrados[0].Value;
                }
                // No manejamos (Handled=true) para que el evento burbujee a Window_KeyDown y avance
            }
            // Flechas (Arriba/Abajo) ya tienen lógica propia abajo...
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                 // (Mantener lógica existente de navegación interna del filtro)
                if (_viewModel.MaterialesFiltrados.Count == 0) return;

                int currentIndex = -1;
                if (_viewModel.MaterialSeleccionado.HasValue)
                {
                    for (int i = 0; i < _viewModel.MaterialesFiltrados.Count; i++)
                    {
                        if (_viewModel.MaterialesFiltrados[i].Value?.ToString() == _viewModel.MaterialSeleccionado.Value.ToString())
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                }

                if (e.Key == Key.Down)
                {
                    currentIndex++;
                    if (currentIndex >= _viewModel.MaterialesFiltrados.Count) currentIndex = 0;
                }
                else if (e.Key == Key.Up)
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = _viewModel.MaterialesFiltrados.Count - 1;
                }

                if (currentIndex >= 0 && currentIndex < _viewModel.MaterialesFiltrados.Count)
                {
                    _viewModel.MaterialSeleccionado = (int?)_viewModel.MaterialesFiltrados[currentIndex].Value;
                    e.Handled = true;
                }
            }
        }

        private void Material_Checked(object sender, RoutedEventArgs e)
        {
            // Auto-avance deshabilitado por solicitud del usuario (usar Tab/Enter)
        }

        private void UnidadMedida_Checked(object sender, RoutedEventArgs e)
        {
             // Auto-avance deshabilitado
        }

        private void Maquinaria_Checked(object sender, RoutedEventArgs e)
        {
             // Auto-avance deshabilitado
        }

        private void SinCompactadora_Checked(object sender, RoutedEventArgs e)
        {
             // Auto-avance deshabilitado. El usuario debe dar Enter o Tab.
        }

        private void TxtTara_GotFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentStep = 5;
        }

        private void LoadingOverlay_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
