using CacelApp;
using CacelApp.Services.Auth;
using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Login;
using System.Net.Mail;
using System.Windows;
using Application = System.Windows.Application;


namespace CacelApp.Views.Modulos.Login;

public partial class LoginModel : ViewModelBase
{
    private readonly MainWindow _mainWindow;
    private readonly IAuthService _authService;
    private readonly ITokenMonitorService _tokenMonitorService;
    public LoginModel() : base()
    {
    }

    public LoginModel(MainWindow mainWindow, IAuthService authService, IDialogService dialogService, ILoadingService loadingService, ITokenMonitorService tokenMonitorService) : base(dialogService, loadingService)
    {
        _mainWindow = mainWindow;
        _authService = authService;
        _tokenMonitorService = tokenMonitorService;
        IngresarCommand = new AsyncRelayCommand(() => ExecuteSafeAsync(IngresarLogicAsync), () => CanLogin);
    }

    // Propiedades enlazables (Bindings)
    [ObservableProperty]
    private string _usuario = "operaciones@companycacel.com";  /*"balanza@companycacel.com";*/

    public bool IsUsuarioValid => IsValidEmail(Usuario);

    private string _contrasena = "Ecoruta25";
    public string Contrasena
    {
        get => _contrasena;
        set
        {
            // Usamos SetProperty para notificar cambios
            if (SetProperty(ref _contrasena, value))
            {
                // Notifica al comando y a CanLogin cada vez que la contraseña cambia.
                IngresarCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanLogin));
            }
        }
    }
    public bool CanLogin => IsUsuarioValid &&
                        !string.IsNullOrWhiteSpace(Contrasena) &&
                        IsNotBusy;

    public IAsyncRelayCommand IngresarCommand { get; }
    partial void OnUsuarioChanged(string value)
    {
        IngresarCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanLogin));
        OnPropertyChanged(nameof(IsUsuarioValid)); // Notificar cambio en IsUsuarioValid
    }
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true;
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    private async Task IngresarLogicAsync()
    {
        var authRequest = new AuthRequest
        {
            username = Usuario,
            password = Contrasena
        };
        var result = await _authService.LoginAsync(authRequest);


        // 💡 INICIAR MONITOREO DEL TOKEN (una vez implementado el servicio)
        _tokenMonitorService.StartMonitoring(result.Data.ExpiresAt);
        // Cargar perfil de usuario automáticamente en la ventana principal
        try
        {
            var mainVm = _mainWindow.DataContext as MainWindowModel;
            if (mainVm != null)
            {
                await mainVm.LoadUserProfileAsync();
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowWarning($"Error al cargar perfil: {ex.Message}", title: "Alerta");   
        }

        // 2. Navegación
        // Cierra la ventana actual (Login)
        Application.Current.Windows.OfType<Login>().FirstOrDefault()?.Close();
        _mainWindow.Show();
    }
}