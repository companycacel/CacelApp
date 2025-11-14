using System.Windows;
using System.Windows.Controls;

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
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is LoginModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Contrasena = passwordBox.Password;
            }
        }
    }
}
