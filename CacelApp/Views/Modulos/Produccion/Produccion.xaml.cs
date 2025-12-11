using System.Windows.Input;
using UserControl = System.Windows.Controls.UserControl;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// Lógica de interacción para Produccion.xaml
/// </summary>
public partial class Produccion : UserControl
{
    private readonly ProduccionModel _viewModel;

    public Produccion(ProduccionModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        
        // Pasar referencia de la vista al ViewModel para restaurar foco
        _viewModel.SetView(this);

        // Cargar datos al inicializar
        Loaded += async (s, e) =>
        {
            await _viewModel.CargarCommand.ExecuteAsync(null);
        };

        // Manejar teclas Enter y Supr
        KeyDown += Produccion_KeyDown;
        
        // Asegurar que el control pueda recibir el foco
        Focusable = true;
        Loaded += (s, e) => RestoreFocus();
    }

    /// <summary>
    /// Restaura el foco al control principal para permitir el uso de teclas Enter/Supr
    /// </summary>
    public void RestoreFocus()
    {
        // Forzar el foco en el UserControl
        Focus();
        // Alternativamente, podríamos dar foco a un elemento específico si fuera necesario
        Keyboard.Focus(this);
    }

    private void Produccion_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Abrir registro rápido con Enter
            if (_viewModel.AbrirRegistroRapidoCommand.CanExecute(null))
            {
                _viewModel.AbrirRegistroRapidoCommand.Execute(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            // Regresar al login con Supr
            if (_viewModel.RegresarLoginCommand.CanExecute(null))
            {
                _viewModel.RegresarLoginCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}
