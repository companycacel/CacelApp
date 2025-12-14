using CacelApp.Services.Auth;
using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Login;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mail;
using Application = System.Windows.Application;


namespace CacelApp.Views.Modulos.Login;

public partial class LoginModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly ITokenMonitorService _tokenMonitorService;
    public LoginModel() : base()
    {
    }

    public LoginModel(IServiceProvider serviceProvider, IAuthService authService, IDialogService dialogService, ILoadingService loadingService, ITokenMonitorService tokenMonitorService) : base(dialogService, loadingService)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;
        _tokenMonitorService = tokenMonitorService;
        IngresarCommand = new AsyncRelayCommand(() => ExecuteSafeAsync(IngresarLogicAsync), () => CanLogin);
    }

    // Propiedades enlazables (Bindings)
    [ObservableProperty]
    private string _usuario = "produccion@companycacel.com";  /*"balanza@companycacel.com";*/

    public bool IsUsuarioValid => IsValidEmail(Usuario);

    private string _contrasena = "00000001";
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
        _tokenMonitorService.StartMonitoring(result.Data.ExpiresAt);
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

        // Cargar perfil de usuario automáticamente en la ventana principal
        try
        {
            var mainVm = mainWindow.DataContext as MainWindowModel;
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
        // Marcar que el cierre es por login exitoso (no por el usuario cerrando con X)
        var loginWindow = Application.Current.Windows.OfType<Login>().FirstOrDefault();
        if (loginWindow != null)
        {
            loginWindow.IsLoginSuccessful = true;
            loginWindow.Close();
        }

        mainWindow.Show();
    }
}