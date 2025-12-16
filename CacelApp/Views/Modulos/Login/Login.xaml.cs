namespace CacelApp.Views.Modulos.Login
{
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        // Flag para saber si el cierre es por login exitoso o por el usuario cerrando con X
        public bool IsLoginSuccessful { get; set; } = false;

        public Login(LoginModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.Closing += Login_Closing;
        }

        private void Login_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Solo cerrar la app si el usuario cerró con X (no por login exitoso)
            if (!IsLoginSuccessful)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is LoginModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.Contrasena = passwordBox.Password;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Si presionan Enter, ejecutar el comando de login
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (this.DataContext is LoginModel viewModel && viewModel.CanLogin)
                {
                    // Ejecutar el comando de login
                    if (viewModel.IngresarCommand.CanExecute(null))
                    {
                        viewModel.IngresarCommand.Execute(null);
                    }
                }
                e.Handled = true;
            }
        }
    }
}
