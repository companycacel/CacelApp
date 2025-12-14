using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CacelApp.Views.Modulos.Balanza;
using CacelApp.Views.Modulos.Configuracion;
using CacelApp.Views.Modulos.Dashboard;
using CacelApp.Views.Modulos.Pesajes;
using CacelApp.Views.Modulos.Produccion;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Profile;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;
using UserControl = System.Windows.Controls.UserControl;


namespace CacelApp;

public partial class MainWindowModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserProfileService _userProfileService;
    private readonly Core.Repositories.Login.IAuthService _authService;
    private readonly Core.Services.Configuration.IConfigurationService _configService;

    /// <summary>
    /// Entorno real del backend (desde GusEnv)
    /// </summary>
    private string? _backendEnvironment;

    [ObservableProperty]
    private bool _isMenuOpen = true;
    public double MenuWidth => IsMenuOpen ? 230 : 60;
    public PackIconKind ToggleMenuIcon => IsMenuOpen ? PackIconKind.ArrowLeft : PackIconKind.ArrowRight;
    /// <summary>
    /// Badge del entorno actual (DEV o PROD)
    /// Prioriza el entorno del backend si está disponible
    /// </summary>
    public string EntornoBadge
    {
        get
        {
            // Si hay entorno del backend configurado, usarlo
            if (!string.IsNullOrEmpty(_backendEnvironment))
            {
                return _backendEnvironment.ToUpper();
            }

            try
            {
                var apiUrl = _configService.GetCurrentApiUrl();
                var appSettings = _configService.LoadAppSettings();

                if (apiUrl == appSettings.ApiUrls.Production)
                    return "PROD";

                return "DEV";
            }
            catch
            {
                return "DEV";
            }
        }
    }
    [ObservableProperty]
    private UserControl _currentView;

    [ObservableProperty]
    private string _currentModuleTitle = "Inicio";

    [ObservableProperty]
    private List<Shared.Entities.MenuItem> _mainMenuItems;

    [ObservableProperty]
    private List<Shared.Entities.MenuItem> _footerMenuItems;

    // Propiedades de Selección
    [ObservableProperty]
    private Shared.Entities.MenuItem _selectedMainMenuItem;

    [ObservableProperty]
    private Shared.Entities.MenuItem _selectedFooterMenuItem;




    [ObservableProperty]
    private string _usuarioEmail = string.Empty;

    [ObservableProperty]
    private string _usuarioNombre = "";

    [ObservableProperty]
    private string _usuarioApellidos = "";

    [ObservableProperty]
    private string _usuarioNombreCompleto = "";

    public ICommand ToggleMenuCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public IAsyncRelayCommand OpenUserProfileCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindowModel(IServiceProvider serviceProvider, IUserProfileService userProfileService, Core.Repositories.Login.IAuthService authService, Core.Services.Configuration.IConfigurationService configService, IDialogService dialogService,
        ILoadingService loadingService) : base(dialogService, loadingService)
    {
        _serviceProvider = serviceProvider;
        _userProfileService = userProfileService;
        _authService = authService;
        _configService = configService;
        ToggleMenuCommand = new RelayCommand(ToggleMenu);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenUserProfileCommand = new AsyncRelayCommand(OpenUserProfile);
        SignOutCommand = new RelayCommand(SignOut);
        ExitCommand = new RelayCommand(Exit);
        InitializeMenuItems();

        _selectedMainMenuItem = _mainMenuItems.First();
        Navigate(_selectedMainMenuItem.ModuleName);
    }
    private void InitializeMenuItems()
    {

        MainMenuItems = new List<Shared.Entities.MenuItem>
        {
            new Shared.Entities.MenuItem { Text = "Inicio", IconKind = PackIconKind.ViewDashboard, ModuleName = "Dashboard" },
            new Shared.Entities.MenuItem { Text = "Balanza", IconKind = PackIconKind.ScaleBalance, ModuleName = "Balanza" },
            new Shared.Entities.MenuItem { Text = "Pesajes", IconKind = PackIconKind.Weight, ModuleName = "Pesajes" },
            new Shared.Entities.MenuItem { Text = "Producción", IconKind = PackIconKind.Factory, ModuleName = "Produccion" }
        };

        FooterMenuItems = new List<Shared.Entities.MenuItem>
        {
            new Shared.Entities.MenuItem { Text = "Configuración", IconKind = PackIconKind.Cog, ModuleName = "Configuracion", Badge = EntornoBadge }
        };
    }
    // --- NOTIFICACIÓN DE CAMBIO ---
    partial void OnIsMenuOpenChanged(bool value)
    {
        // Notificar el cambio de las propiedades dependientes
        OnPropertyChanged(nameof(MenuWidth));
        OnPropertyChanged(nameof(ToggleMenuIcon));
    }

    partial void OnUsuarioNombreChanged(string value)
    {
        ActualizarNombreCompleto();
    }

    partial void OnUsuarioApellidosChanged(string value)
    {
        ActualizarNombreCompleto();
    }

    private void ActualizarNombreCompleto()
    {
        UsuarioNombreCompleto = $"{UsuarioNombre} {UsuarioApellidos}".Trim();
    }

    // --- MANEJO DE SELECCIÓN Y NAVEGACIÓN ---

    partial void OnSelectedMainMenuItemChanged(Shared.Entities.MenuItem value)
    {
        if (value != null)
        {
            SelectedFooterMenuItem = null;
            Navigate(value.ModuleName);
        }
    }

    partial void OnSelectedFooterMenuItemChanged(Shared.Entities.MenuItem value)
    {
        if (value != null)
        {
            SelectedMainMenuItem = null;
            Navigate(value.ModuleName);
        }
    }

    private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    private void Navigate(string moduleName)
    {
        CurrentModuleTitle = moduleName switch
        {
            "Dashboard" => "Dashboard de Servicios",
            "Balanza" => "Gestión de Balanza",
            "Pesajes" => "Documentos y Pesajes",
            "Produccion" => "Gestión de Producción",
            "Configuracion" => "Configuración del Sistema",
            _ => moduleName
        };

        CurrentView = moduleName switch
        {
            "Dashboard" => _serviceProvider.GetRequiredService<Dashboard>(),
            "Balanza" => _serviceProvider.GetRequiredService<Balanza>(),
            "Pesajes" => _serviceProvider.GetRequiredService<Pesajes>(),
            "Produccion" => _serviceProvider.GetRequiredService<Produccion>(),
            "Configuracion" => _serviceProvider.GetRequiredService<Configuracion>(),
            _ => null // O una vista de error/vacía
        };
        //IsMenuOpen = false;
    }

    private void ToggleTheme()
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();

        // Alternar entre el tema Dark y Light
        var baseTheme = theme.GetBaseTheme() == BaseTheme.Dark ? BaseTheme.Light : BaseTheme.Dark;

        theme.SetBaseTheme(baseTheme);
        paletteHelper.SetTheme(theme);

        if (baseTheme == BaseTheme.Dark)
        {
            // Custom Black Mode #121212 para el fondo principal
            var blackBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x12, 0x12, 0x12));

            // Color #1e1e1e para las tablas y cards
            var tableBackgroundBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x1e, 0x1e, 0x1e));

            // Modificar solo el fondo principal en modo oscuro
            Application.Current.Resources["MaterialDesignPaper"] = blackBrush;
            Application.Current.Resources["MaterialDesignBackground"] = blackBrush;
            Application.Current.Resources["MaterialDesignCardBackground"] = tableBackgroundBrush;
        }
        else
        {
            // Restaurar colores por defecto del tema claro
            // Remover las sobrescrituras para que use los valores del tema
            Application.Current.Resources.Remove("MaterialDesignPaper");
            Application.Current.Resources.Remove("MaterialDesignBackground");
            Application.Current.Resources.Remove("MaterialDesignCardBackground");
        }
    }

    public async Task LoadUserProfileAsync()
    {
        var profileResponse = await _userProfileService.GetUserProfileAsync();

        if (profileResponse?.Data != null)
        {
            UsuarioEmail = profileResponse.Data.gus_user ?? "No disponible";
            UsuarioNombre = profileResponse.Data.gpe?.gpe_nombre ?? "No disponible";
            UsuarioApellidos = profileResponse.Data.gpe?.gpe_apellidos ?? "";

            // Validar coherencia entre entorno configurado y entorno real del backend
            await ValidateEnvironmentAsync(profileResponse.Data.gus_env);
        }
    }

    // Método público seguro para navegar al Dashboard desde vistas externas
    public void NavigateToDashboard()
    {
        Navigate("Dashboard");
    }

    private async Task OpenUserProfile()
    {
        try
        {
            var profileResponse = await _userProfileService.GetUserProfileAsync();

            if (profileResponse?.Data != null)
            {
                // Create the view and bind the profile data as its DataContext
                var view = _serviceProvider.GetRequiredService<Views.Modulos.Profile.UserProfile>();
                view.DataContext = profileResponse.Data;

                // Show the profile view in the main content area
                CurrentModuleTitle = "Perfil";
                CurrentView = view;
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al abrir perfil: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida que el entorno configurado coincida con el entorno real del backend
    /// </summary>
    private async Task ValidateEnvironmentAsync(string? backendEnv)
    {
        if (string.IsNullOrEmpty(backendEnv))
            return;

        // Normalizar entorno del backend (por si viene en minúsculas)
        var normalizedBackendEnv = backendEnv.ToUpper() == "SICA" ? "PROD" : "DEV";

        // Obtener entorno configurado en la app
        var apiUrl = _configService.GetCurrentApiUrl();
        var appSettings = _configService.LoadAppSettings();
        var configuredEnv = (apiUrl == appSettings.ApiUrls.Production) ? "PROD" : "DEV";

        // Si no coinciden, mostrar alerta
        if (normalizedBackendEnv != configuredEnv)
        {
            var continuar = await DialogService.ShowConfirm(
                $"El entorno configurado en la aplicación es '{configuredEnv}', " +
                $"pero la API está conectada a la base de datos de '{normalizedBackendEnv}'.\n\n" +
                $"Esto puede causar problemas de trazabilidad.\n\n" +
                $"¿Desea continuar usando el entorno real del backend ('{normalizedBackendEnv}')?\n\n" +
                $"• Continuar: El badge mostrará '{normalizedBackendEnv}'\n" +
                $"• Cancelar: Salir y volver al login",
                "Advertencia: Inconsistencia de Entorno",
                "Continuar");

            if (continuar)
            {
                // Usuario eligió continuar - actualizar badge con entorno real
                _backendEnvironment = normalizedBackendEnv;
                OnPropertyChanged(nameof(EntornoBadge));

                // Actualizar el badge en el menú de configuración
                var configMenuItem = FooterMenuItems.FirstOrDefault(m => m.ModuleName == "Configuracion");
                if (configMenuItem != null)
                {
                    configMenuItem.Badge = normalizedBackendEnv;
                }
            }
            else
            {
                // Usuario eligió salir - cerrar sesión y volver al login
                SignOut();
            }
        }
        else
        {
            // Los entornos coinciden - guardar por si acaso
            _backendEnvironment = normalizedBackendEnv;
        }
    }

    /// <summary>
    /// Cierra sesión y vuelve al login (sin cerrar la app)
    /// </summary>
    private async void SignOut()
    {
        try
        {
            await _authService.LogoutAsync();
        }
        catch { }

        // Cerrar la ventana principal primero (marcar como logout para evitar Shutdown)
        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow != null)
        {
            mainWindow.IsLogoutInProgress = true;
            mainWindow.Close();
        }

        // Cerrar cualquier ventana de Login existente para evitar conflictos de DialogHost
        var existingLoginWindows = Application.Current.Windows.OfType<Views.Modulos.Login.Login>().ToList();
        foreach (var loginWindow in existingLoginWindows)
        {
            // Marcar como cierre intencional para que no llame a Shutdown()
            loginWindow.IsLoginSuccessful = true;
            loginWindow.Close();
        }

        // Crear y mostrar nueva ventana de Login
        try
        {
            var login = _serviceProvider.GetRequiredService<Views.Modulos.Login.Login>();
            login.Show();
        }
        catch { }
    }

    /// <summary>
    /// Cierra la aplicación completamente
    /// </summary>
    private async void Exit()
    {
        var result = await DialogService.ShowConfirm(
            "¿Está seguro que desea salir de la aplicación?",
            "Salir de la Aplicación",
            "Sí, Salir",
            "Cancelar");

        if (result)
        {
            // Cerrar sesión primero
            try
            {
                await _authService.LogoutAsync();
            }
            catch { }

            // Cerrar todas las ventanas y la aplicación
            Application.Current.Shutdown();
        }
    }
}