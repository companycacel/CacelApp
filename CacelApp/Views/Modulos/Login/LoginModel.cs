using CacelApp.Services.Auth;
using CacelApp.Services.Update;
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
    private readonly Core.Services.Configuration.IConfigurationService _configService;
    private readonly IUpdateService _updateService;

    public LoginModel() : base()
    {
    }

    public LoginModel(IServiceProvider serviceProvider, IAuthService authService, IDialogService dialogService, ILoadingService loadingService, ITokenMonitorService tokenMonitorService, Core.Services.Configuration.IConfigurationService configService, IUpdateService updateService) : base(dialogService, loadingService)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;
        _tokenMonitorService = tokenMonitorService;
        _configService = configService;
        _updateService = updateService;
        IngresarCommand = new AsyncRelayCommand(() => ExecuteSafeAsync(IngresarLogicAsync), () => CanLogin);

        AppVersion = $"v{_updateService.CurrentVersion}";

        // Cargar último usuario desde config.json
        _ = CargarUltimoUsuarioAsync();

        // Verificar actualizaciones al inicio
        _ = VerificarUpdateAlInicioAsync();
    }

    /// <summary>
    /// Verifica si hay actualizaciones disponibles de forma automática al iniciar
    /// </summary>
    private async Task VerificarUpdateAlInicioAsync()
    {
        try
        {
            // Esperar un momento para que la ventana cargue completamente
            await Task.Delay(2000);

            var updateInfo = await _updateService.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                var message = $"Hay una nueva versión disponible: {updateInfo.Version}\n\n" +
                             $"Tamaño: {updateInfo.FormattedSize}\n\n" +
                             $"¿Desea descargar e instalar la actualización ahora?";

                var shouldUpdate = await DialogService.ShowConfirm(
                    message,
                    "Actualización Disponible",
                    "Descargar e Instalar",
                    "Continuar");

                if (shouldUpdate)
                {
                    LoadingService.StartLoading();
                    await _updateService.DownloadAndInstallUpdateAsync(updateInfo);
                    LoadingService.StopLoading();

                    // Esperar un momento antes de aplicar (más confiable)
                    await Task.Delay(500);

                    // Aplicar y reiniciar automáticamente
                    await _updateService.ApplyUpdatesAndRestartAsync();

                    // Por si acaso el reinicio no es instantáneo
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en verificación automática: {ex.Message}");
        }
    }

    // Propiedades enlazables (Bindings)
    [ObservableProperty]
    private string _usuario = string.Empty;  // Se cargará desde config.json

    [ObservableProperty]
    private string _appVersion = "v1.0.0";

    public bool IsUsuarioValid => IsValidEmail(Usuario);

    private string _contrasena = "mobile";
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

        // Guardar el usuario para futuras recomendaciones
        await GuardarUltimoUsuarioAsync();

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

    /// <summary>
    /// Carga el último usuario usado desde config.json
    /// </summary>
    private async Task CargarUltimoUsuarioAsync()
    {
        try
        {
            var config = await _configService.LoadAsync();
            if (!string.IsNullOrEmpty(config.LastUsername))
            {
                Usuario = config.LastUsername;
            }
            else
            {
                // Si no hay usuario guardado, usar el default de producción
                Usuario = "produccion@companycacel.com";
            }
        }
        catch
        {
            // Si falla, usar el default
            Usuario = "produccion@companycacel.com";
        }
    }

    /// <summary>
    /// Guarda el usuario actual en config.json para futuras recomendaciones
    /// </summary>
    private async Task GuardarUltimoUsuarioAsync()
    {
        try
        {
            var config = await _configService.LoadAsync();
            config.LastUsername = Usuario;
            await _configService.SaveAsync(config);
        }
        catch
        {
            // Si falla al guardar, no es crítico, simplemente no se guardará la recomendación
        }
    }
}