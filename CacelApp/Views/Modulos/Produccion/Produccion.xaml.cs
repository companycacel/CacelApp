using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace CacelApp.Views.Modulos.Produccion;

/// <summary>
/// Lógica de interacción para Produccion.xaml
/// </summary>
public partial class Produccion : UserControl
{
    public Produccion(ProduccionModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Cargar datos al inicializar
        Loaded += async (s, e) =>
        {
            await viewModel.CargarCommand.ExecuteAsync(null);
        };
    }
}
