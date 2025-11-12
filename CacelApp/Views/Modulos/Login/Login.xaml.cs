using System.Windows;

namespace CacelApp.Views.Modulos.Login
{
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login(LoginModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
