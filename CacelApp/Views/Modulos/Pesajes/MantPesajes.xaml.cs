using CacelApp.Shared.Controls.Form;

namespace CacelApp.Views.Modulos.Pesajes;

/// <summary>
/// Interaction logic for MantPesajes.xaml
/// </summary>
public partial class MantPesajes : Window
{
    public MantPesajes()
    {
        InitializeComponent();

        // Conectar el RequestClose del ViewModel con el método Close de la ventana
        Loaded += (s, e) =>
        {
            if (DataContext is MantPesajesModel viewModel)
            {
                viewModel.RequestClose = (success) => 
                {
                    if (success) this.DialogResult = true;
                    this.Close();
                };
            }
        };

        // Limpiar recursos al cerrar
        Closed += (s, e) =>
        {
            if (DataContext is MantPesajesModel viewModel)
            {
                viewModel.Cleanup();
            }
        };
    }
}
