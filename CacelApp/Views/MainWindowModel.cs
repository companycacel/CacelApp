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
    private readonly Services.Update.IUpdateService _updateService;

    private string? _backendEnvironment;

    [ObservableProperty]
    private bool _isMenuOpen = true;
    public double MenuWidth => IsMenuOpen ? 230 : 60;
    public string EntornoBadge
    {
        get
        {
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

    [ObservableProperty]
    private string _appVersion = "v1.0.0";

    public ICommand ToggleMenuCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public IAsyncRelayCommand OpenUserProfileCommand { get; }
    public IAsyncRelayCommand CheckForUpdatesCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindowModel(IServiceProvider serviceProvider, IUserProfileService userProfileService, Core.Repositories.Login.IAuthService authService, Core.Services.Configuration.IConfigurationService configService,
        Services.Update.IUpdateService updateService, IDialogService dialogService, ILoadingService loadingService) : base(dialogService, loadingService)
    {
        _serviceProvider = serviceProvider;
        _userProfileService = userProfileService;
        _authService = authService;
        _configService = configService;
        _updateService = updateService;
        ToggleMenuCommand = new RelayCommand(ToggleMenu);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenUserProfileCommand = new AsyncRelayCommand(OpenUserProfile);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdates);
        SignOutCommand = new RelayCommand(SignOut);
        ExitCommand = new RelayCommand(Exit);
        InitializeMenuItems();

        _selectedMainMenuItem = _mainMenuItems.First();
        Navigate(_selectedMainMenuItem.ModuleName);

        AppVersion = $"v{_updateService.CurrentVersion}";
    }
    private void InitializeMenuItems()
    {
        // El Dashboard e Inicio son estáticos (siempre visibles)
        MainMenuItems = new List<Shared.Entities.MenuItem>
        {
            new Shared.Entities.MenuItem { Text = "Inicio", IconKind = PackIconKind.ViewDashboard, ModuleName = "Dashboard" }
        };

        // El pie de página se llenará dinámicamente si el usuario tiene permisos (ej: Configuración)
        FooterMenuItems = new List<Shared.Entities.MenuItem>();
    }
    partial void OnIsMenuOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(MenuWidth));
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
            _ => null
        };
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
            var blackBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x12, 0x12, 0x12));

            var tableBackgroundBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x1e, 0x1e, 0x1e));

            Application.Current.Resources["MaterialDesignPaper"] = blackBrush;
            Application.Current.Resources["MaterialDesignBackground"] = blackBrush;
            Application.Current.Resources["MaterialDesignCardBackground"] = tableBackgroundBrush;
        }
        else
        {
            Application.Current.Resources.Remove("MaterialDesignPaper");
            Application.Current.Resources.Remove("MaterialDesignBackground");
            Application.Current.Resources.Remove("MaterialDesignCardBackground");
        }
    }

    public async Task LoadUserProfileAsync()
    {
        var profileResponse = await _userProfileService.GetUserProfileAsync();

        if (profileResponse != null)
        {
            UsuarioEmail = profileResponse.gus_user ?? "No disponible";
            UsuarioNombre = profileResponse.gpe?.gpe_nombre ?? "No disponible";
            UsuarioApellidos = profileResponse.gpe?.gpe_apellidos ?? "";

            // Validar coherencia entre entorno configurado y entorno real del backend
            await ValidateEnvironmentAsync(profileResponse.gus_env);

            // Cargar permisos dinámicos
            await LoadPermisosAsync();
        }
    }

    private async Task LoadPermisosAsync()
    {
        try
        {
            var permisos = await _userProfileService.GetPermisosAsync();

            if (permisos != null && permisos.Any())
            {
                var newMenuItems = new List<Shared.Entities.MenuItem>
                {
                    new Shared.Entities.MenuItem { Text = "Inicio", IconKind = PackIconKind.ViewDashboard, ModuleName = "Dashboard" }
                };

                var newFooterItems = new List<Shared.Entities.MenuItem>();

                foreach (var permiso in permisos.OrderBy(p => p.order))
                {
                    var iconKind = PackIconKind.Help;
                    var iconName = permiso.icon;

                    if (iconName.StartsWith("PackIconKind."))
                    {
                        iconName = iconName.Replace("PackIconKind.", "");
                    }

                    if (Enum.TryParse<PackIconKind>(iconName, out var parsedIcon))
                    {
                        iconKind = parsedIcon;
                    }

                    var moduleName = permiso.path;
                    if (!string.IsNullOrEmpty(moduleName))
                    {
                        moduleName = char.ToUpper(moduleName[0]) + moduleName.Substring(1).ToLower();
                    }

                    var menuItem = new Shared.Entities.MenuItem
                    {
                        Text = char.ToUpper(permiso.title[0]) + permiso.title.Substring(1).ToLowerInvariant(),
                        IconKind = iconKind,
                        ModuleName = moduleName
                    };

                    // Si el módulo es configuración, lo movemos al footer y le asignamos el badge del entorno
                    if (moduleName == "Configuracion")
                    {
                        menuItem.Badge = EntornoBadge;
                        newFooterItems.Add(menuItem);
                    }
                    else
                    {
                        newMenuItems.Add(menuItem);
                    }
                }

                MainMenuItems = newMenuItems;
                FooterMenuItems = newFooterItems;
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowWarning($"Error al cargar permisos de módulos: {ex.Message}", "Permisos");
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

            if (profileResponse != null)
            {
                var view = _serviceProvider.GetRequiredService<Views.Modulos.Profile.UserProfile>();
                // UserProfile binds to both gus_user and nested gpe fields.
                view.DataContext = profileResponse;

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

        var normalizedBackendEnv = backendEnv.ToUpper() == "SICA" ? "PROD" : "DEV";

        var apiUrl = _configService.GetCurrentApiUrl();
        var appSettings = _configService.LoadAppSettings();
        var configuredEnv = (apiUrl == appSettings.ApiUrls.Production) ? "PROD" : "DEV";

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
                _backendEnvironment = normalizedBackendEnv;
                OnPropertyChanged(nameof(EntornoBadge));

                var configMenuItem = FooterMenuItems.FirstOrDefault(m => m.ModuleName == "Configuracion");
                if (configMenuItem != null)
                {
                    configMenuItem.Badge = normalizedBackendEnv;
                }
            }
            else
            {
                SignOut();
            }
        }
        else
        {
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

        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow != null)
        {
            mainWindow.IsLogoutInProgress = true;
            mainWindow.Close();
        }

        var existingLoginWindows = Application.Current.Windows.OfType<Views.Modulos.Login.Login>().ToList();
        foreach (var loginWindow in existingLoginWindows)
        {
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
    /// Verifica si hay actualizaciones disponibles
    /// </summary>
    private async Task CheckForUpdates()
    {
        try
        {
            LoadingService.StartLoading();

            var updateInfo = await _updateService.CheckForUpdatesAsync();

            LoadingService.StopLoading();

            if (updateInfo == null)
            {
                await DialogService.ShowInfo(
                    "No hay actualizaciones disponibles. Ya tienes la última versión instalada.",
                    "Actualizaciones");
                return;
            }

            var message = $"Hay una nueva versión disponible: {updateInfo.Version}\n\n" +
                         $"Tamaño: {updateInfo.FormattedSize}\n\n" +
                         $"¿Desea descargar e instalar la actualización ahora?";

            if (!string.IsNullOrEmpty(updateInfo.ReleaseNotes))
            {
                message += $"\n\nNotas de la versión:\n{updateInfo.ReleaseNotes}";
            }

            var shouldUpdate = await DialogService.ShowConfirm(
                message,
                "Actualización Disponible",
                "Descargar e Instalar",
                "Cancelar");

            if (!shouldUpdate)
                return;

            // Descargar e instalar la actualización
            LoadingService.StartLoading();

            await _updateService.DownloadAndInstallUpdateAsync(updateInfo);

            LoadingService.StopLoading();

            // Preguntar si desea reiniciar ahora
            var shouldRestart = await DialogService.ShowConfirm(
                "La actualización se ha descargado correctamente.\n\n" +
                "¿Desea reiniciar la aplicación ahora para aplicar la actualización?",
                "Actualización Lista",
                "Reiniciar Ahora",
                "Reiniciar Más Tarde");

            if (shouldRestart)
            {
                // Aplicar actualización y reiniciar
                await _updateService.ApplyUpdatesAndRestartAsync();
            }
        }
        catch (Exception ex)
        {
            LoadingService.StopLoading();
            await DialogService.ShowError(
                $"Error al verificar actualizaciones: {ex.Message}",
                "Error de Actualización");
        }
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
