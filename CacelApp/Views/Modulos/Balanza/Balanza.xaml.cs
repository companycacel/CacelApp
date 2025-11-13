using System.Windows.Controls;


namespace CacelApp.Views.Modulos.Balanza
{
    /// <summary>
    /// Lógica de interacción para Balanza.xaml
    /// </summary>
    public partial class Balanza : UserControl
    {
        public Balanza(BalanzaModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
