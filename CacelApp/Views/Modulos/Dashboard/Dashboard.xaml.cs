using System.Windows.Controls;


namespace CacelApp.Views.Modulos.Dashboard
{
    /// <summary>
    /// Lógica de interacción para Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard(DashboardModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
