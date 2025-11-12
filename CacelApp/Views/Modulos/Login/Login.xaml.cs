using System.Windows;

namespace CacelApp.Modulos.Login
{
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
