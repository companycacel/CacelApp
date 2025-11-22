using System.Windows;

namespace CacelApp.Views.Modulos.Balanza;

/// <summary>
/// Ventana de mantenimiento para registros de Balanza
/// Permite crear y editar registros de pesaje con todas sus validaciones
/// </summary>
public partial class MantBalanza : Window
{
    public MantBalanza()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Constructor con ViewModel inyectado
    /// </summary>
    public MantBalanza(MantBalanzaModel viewModel) : this()
    {
        if (viewModel == null)
            throw new System.ArgumentNullException(nameof(viewModel));

        DataContext = viewModel;
        
        // Asignar la referencia de la ventana al ViewModel
        viewModel.SetWindow(this);
        
        // Cargar datos después de que la ventana esté visible
        // Solo si no se han cargado previamente (modo edición carga antes)
        Loaded += async (s, e) =>
        {
            // Si estamos en modo edición, los datos ya fueron cargados
            if (!viewModel.EsEdicion)
            {
                await viewModel.CargarDatosInicialesAsync();
            }
        };
    }
}
