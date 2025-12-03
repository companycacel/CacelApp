using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// Lógica de interacción para RegistroRapidoProduccion.xaml
/// </summary>
public partial class RegistroRapidoProduccion : Window
{
    private readonly RegistroRapidoProduccionModel _viewModel;

    public RegistroRapidoProduccion(RegistroRapidoProduccionModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Manejar tecla Enter para guardar
        KeyDown += Window_KeyDown;
        Closed += Window_Closed;
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Guardar cuando se presiona Enter
            if (_viewModel.GuardarCommand.CanExecute(null))
            {
                _viewModel.GuardarCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _viewModel.Cleanup();
    }

    private void MaterialButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag != null)
        {
            var materialId = int.Parse(button.Tag.ToString()!);
            _viewModel.SeleccionarMaterialCommand.Execute(materialId);
        }
    }

    private void TipoEmpaque_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton radioButton && radioButton.Tag != null)
        {
            _viewModel.TipoEmpaqueSeleccionado = radioButton.Tag.ToString();
        }
    }

    private void Peso_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Actualizar cálculo de peso neto
        _viewModel.ActualizarPesosCommand.Execute(null);
    }

    private void LoadingOverlay_Loaded(object sender, RoutedEventArgs e)
    {
        // Event handler for LoadingOverlay loaded event
        // This can be used for initialization if needed
    }
}
