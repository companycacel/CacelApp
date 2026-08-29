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
                        Dispatcher.InvokeAsync(() => FocusMaterialFilterBox(), DispatcherPriority.Input);
                    }
                }
            };
        }

        private void FocusMaterialFilterBox()
        {
            if (_viewModel.EsCvSolo)
            {
                MaterialFilterBoxCvSolo?.Focus();
                MaterialFilterBoxCvSolo?.SelectAll();
            }
            else if (_viewModel.EsInSolo)
            {
                MaterialFilterBoxInSolo?.Focus();
                MaterialFilterBoxInSolo?.SelectAll();
            }
            else if (_viewModel.EsTransformacion)
            {
                if (!_viewModel.MaterialCvSeleccionado.HasValue)
                {
                    MaterialCvFilterBox?.Focus();
                    MaterialCvFilterBox?.SelectAll();
                }
                else
                {
                    MaterialInFilterBox?.Focus();
                    MaterialInFilterBox?.SelectAll();
                }
            }
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
                FocusMaterialFilterBox();
                e.Handled = true;
                return;
            }

            // Navegación con TAB
            if (e.Key == Key.Tab)
            {
                if (_viewModel.CurrentStep == 1 && _viewModel.PesoBruto > 0)
                {
                    _viewModel.CurrentStep = 2;
                    Dispatcher.InvokeAsync(() => FocusMaterialFilterBox(), DispatcherPriority.Input);
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 2)
                {
                    if (_viewModel.EsTransformacion && _viewModel.MaterialCvSeleccionado.HasValue && !_viewModel.MaterialInSeleccionado.HasValue)
                    {
                        Dispatcher.InvokeAsync(() => {
                            MaterialInFilterBox?.Focus();
                            MaterialInFilterBox?.SelectAll();
                        }, DispatcherPriority.Input);
                        e.Handled = true;
                    }
                    else if ((_viewModel.EsCvSolo && _viewModel.MaterialCvSeleccionado.HasValue) ||
                             (_viewModel.EsInSolo && _viewModel.MaterialInSeleccionado.HasValue) ||
                             (_viewModel.EsTransformacion && _viewModel.MaterialCvSeleccionado.HasValue && _viewModel.MaterialInSeleccionado.HasValue))
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
                        Dispatcher.InvokeAsync(() => FocusMaterialFilterBox(), DispatcherPriority.Input);
                    }
                    else if (_viewModel.PrimeraBalanza?.CapturarCommand.CanExecute(null) == true)
                    {
                        _viewModel.PrimeraBalanza.CapturarCommand.Execute(null);
                    }
                    e.Handled = true;
                }
                else if (_viewModel.CurrentStep == 2)
                {
                    if (_viewModel.EsTransformacion && _viewModel.MaterialCvSeleccionado.HasValue && !_viewModel.MaterialInSeleccionado.HasValue)
                    {
                        Dispatcher.InvokeAsync(() => {
                            MaterialInFilterBox?.Focus();
                            MaterialInFilterBox?.SelectAll();
                        }, DispatcherPriority.Input);
                        e.Handled = true;
                    }
                    else if ((_viewModel.EsCvSolo && _viewModel.MaterialCvSeleccionado.HasValue) ||
                             (_viewModel.EsInSolo && _viewModel.MaterialInSeleccionado.HasValue) ||
                             (_viewModel.EsTransformacion && _viewModel.MaterialCvSeleccionado.HasValue && _viewModel.MaterialInSeleccionado.HasValue))
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

        private void MaterialCvFilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel.MaterialesCvFiltrados.Count > 0)
            {
                if (!_viewModel.MaterialCvSeleccionado.HasValue)
                {
                     _viewModel.MaterialCvSeleccionado = (int?)_viewModel.MaterialesCvFiltrados[0].Value;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (_viewModel.MaterialesCvFiltrados.Count == 0) return;

                int currentIndex = -1;
                if (_viewModel.MaterialCvSeleccionado.HasValue)
                {
                    for (int i = 0; i < _viewModel.MaterialesCvFiltrados.Count; i++)
                    {
                        if (_viewModel.MaterialesCvFiltrados[i].Value?.ToString() == _viewModel.MaterialCvSeleccionado.Value.ToString())
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                }

                if (e.Key == Key.Down)
                {
                    currentIndex++;
                    if (currentIndex >= _viewModel.MaterialesCvFiltrados.Count) currentIndex = 0;
                }
                else if (e.Key == Key.Up)
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = _viewModel.MaterialesCvFiltrados.Count - 1;
                }

                if (currentIndex >= 0 && currentIndex < _viewModel.MaterialesCvFiltrados.Count)
                {
                    _viewModel.MaterialCvSeleccionado = (int?)_viewModel.MaterialesCvFiltrados[currentIndex].Value;
                    e.Handled = true;
                }
            }
        }

        private void MaterialInFilterBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _viewModel.MaterialesInFiltrados.Count > 0)
            {
                if (!_viewModel.MaterialInSeleccionado.HasValue)
                {
                    _viewModel.MaterialInSeleccionado = (int?)_viewModel.MaterialesInFiltrados[0].Value;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (_viewModel.MaterialesInFiltrados.Count == 0) return;

                int currentIndex = -1;
                if (_viewModel.MaterialInSeleccionado.HasValue)
                {
                    for (int i = 0; i < _viewModel.MaterialesInFiltrados.Count; i++)
                    {
                        if (_viewModel.MaterialesInFiltrados[i].Value?.ToString() == _viewModel.MaterialInSeleccionado.Value.ToString())
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                }

                if (e.Key == Key.Down)
                {
                    currentIndex++;
                    if (currentIndex >= _viewModel.MaterialesInFiltrados.Count) currentIndex = 0;
                }
                else if (e.Key == Key.Up)
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = _viewModel.MaterialesInFiltrados.Count - 1;
                }

                if (currentIndex >= 0 && currentIndex < _viewModel.MaterialesInFiltrados.Count)
                {
                    _viewModel.MaterialInSeleccionado = (int?)_viewModel.MaterialesInFiltrados[currentIndex].Value;
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

        private void TxtTara_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (System.Windows.Controls.TextBox)sender;

            string textoPropuesto = textBox.Text.Insert(
                textBox.SelectionStart,
                e.Text
            );

            // Permitir números con hasta 2 decimales, aceptando punto o coma
            e.Handled = !System.Text.RegularExpressions.Regex
                .IsMatch(textoPropuesto, @"^\d*([.,]\d{0,2})?$");
        }
    }
}
