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

            KeyDown += Window_KeyDown;
            Closed += Window_Closed;
            
            Loaded += async (s, e) => {
                 await System.Threading.Tasks.Task.Delay(500); 
                 if (VisorControl != null) VisorControl.Focus();
                 _viewModel.CurrentStep = 1;
                 _isInitializing = false; 
            };
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _viewModel.Cleanup();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                _viewModel.CurrentStep = 2;
                MaterialFilterBox.Focus();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                // Si estamos en el paso final, Guardar
                if (_viewModel.CurrentStep >= 4 && _viewModel.CanSave)
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
                        Dispatcher.InvokeAsync(() => MaterialFilterBox.Focus(), DispatcherPriority.Input);
                    }
                    else if (_viewModel.PrimeraBalanza?.CapturarCommand.CanExecute(null) == true)
                    {
                        _viewModel.PrimeraBalanza.CapturarCommand.Execute(null);
                    }
                    e.Handled = true;
                }
            }
        }

        private void MaterialFilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (_viewModel.MaterialesFiltrados.Count > 0 && _viewModel.MaterialSeleccionado.HasValue)
                {
                    // Confirmar y saltar al siguiente paso
                    Material_Checked(null, null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && _viewModel.MaterialesFiltrados.Count > 0)
                {
                    _viewModel.MaterialSeleccionado = (int?)_viewModel.MaterialesFiltrados[0].Value;
                    Material_Checked(null, null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
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
            if (_isInitializing) return;

            // Si el foco está en el buscador de materiales, bloqueamos el salto automático.
            if (sender != null && MaterialFilterBox.IsFocused) return;

            _viewModel.CurrentStep = 3;
            Dispatcher.InvokeAsync(() => GroupUnidad.Focus(), DispatcherPriority.Background);
        }

        private void UnidadMedida_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            // Protección: No robar el foco si estamos buscando material
            if (MaterialFilterBox.IsFocused) return;

            _viewModel.CurrentStep = 4;
            Dispatcher.InvokeAsync(() => GroupMaquinaria.Focus(), DispatcherPriority.Background);
        }

        private void Maquinaria_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            // Protección: No robar el foco si estamos buscando material
            if (MaterialFilterBox.IsFocused) return;

            _viewModel.CurrentStep = 5;
            Dispatcher.InvokeAsync(() => {
                TxtTara.Focus();
                TxtTara.SelectAll(); 
            }, DispatcherPriority.Background);
        }

        private void SinCompactadora_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _viewModel.CurrentStep = 5;
            Dispatcher.InvokeAsync(() => {
                TxtTara.Focus();
                TxtTara.SelectAll(); 
            }, DispatcherPriority.Background);
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
