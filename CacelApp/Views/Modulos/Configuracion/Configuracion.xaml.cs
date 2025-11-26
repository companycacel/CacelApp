
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace CacelApp.Views.Modulos.Configuracion
{
    /// <summary>
    /// Lógica de interacción para Configuracion.xaml
    /// </summary>
    public partial class Configuracion : UserControl
    {
        public Configuracion(ConfiguracionModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
