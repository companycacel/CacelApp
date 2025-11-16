using System.Windows;

namespace CacelApp.Views.Modulos.Balanza;

/// <summary>
/// Ventana de mantenimiento para registros de Balanza
/// Permite crear y editar registros de pesaje con todas sus validaciones
/// </summary>
public partial class MantBalanzaWindow : Window
{
    public MantBalanzaWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor con ViewModel inyectado
    /// </summary>
    public MantBalanzaWindow(MantBalanzaViewModel viewModel) : this()
    {
        if (viewModel == null)
            throw new System.ArgumentNullException(nameof(viewModel));

        DataContext = viewModel;
        
        // Asignar la referencia de la ventana al ViewModel
        viewModel.SetWindow(this);
    }
}
