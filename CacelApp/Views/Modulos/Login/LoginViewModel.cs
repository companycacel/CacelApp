using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace CacelApp.Modulos.Login
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly MainWindow _mainWindow;
        // private readonly IAuthService _authService; 

        // Constructor sin parámetros para el designer de XAML
        public LoginViewModel() : this(null)
        {
        }

        // Inyectamos la Ventana Principal (MainWindow) para poder mostrarla
        public LoginViewModel(MainWindow mainWindow /*, IAuthService authService*/)
        {
            _mainWindow = mainWindow;
            // _authService = authService;
            IngresarCommand = new AsyncRelayCommand(IngresarAsync);
        }

    // Propiedades enlazables (Bindings)
    [ObservableProperty]
    private string _usuario = "balanza@companycacel.com"; //

    [ObservableProperty]
    private string _contrasena = "mobile"; // Esta propiedad NO DEBE usarse en producción por seguridad (usar SecureString)

    [ObservableProperty]
    private bool _isBusy = false; // Para el estado del botón/spinner

    public bool IsNotBusy => !IsBusy;

    public IAsyncRelayCommand IngresarCommand { get; }

    private async Task IngresarAsync()
    {
        IsBusy = true;

        // 1. Lógica de Autenticación
        // var result = await _authService.LoginAsync(Usuario, Contrasena);

        // Simulación de autenticación exitosa:
        await Task.Delay(1500);

        // 2. Navegación
        if (Usuario == "balanza@companycacel.com") // && result.IsSuccess
        {
            // Cierra la ventana actual (LoginView)
            Application.Current.Windows.OfType<Login>().FirstOrDefault()?.Close();

            // Muestra la ventana principal (MainWindow)
            _mainWindow.Show();
        }
        else
        {
            // Mostrar mensaje de error (usar un DialogHost de MaterialDesign para un look moderno)
            MessageBox.Show("Credenciales inválidas.");
        }

        IsBusy = false;
    }
}
}