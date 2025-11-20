
using System.Windows.Controls;

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
