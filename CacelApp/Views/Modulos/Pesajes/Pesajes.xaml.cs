using System.Windows.Controls;

namespace CacelApp.Views.Modulos.Pesajes;

/// <summary>
/// Lógica de interacción para Pesajes.xaml
/// </summary>
public partial class Pesajes : UserControl
{
    public Pesajes(PesajesModel viewModel)
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
