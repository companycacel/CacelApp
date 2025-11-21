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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace CacelApp;

public partial class MainWindowModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserProfileService _userProfileService;
    private readonly Core.Repositories.Login.IAuthService _authService;

    [ObservableProperty]
    private bool _isMenuOpen = true;
    public double MenuWidth => IsMenuOpen ? 230 : 60;
    public PackIconKind ToggleMenuIcon => IsMenuOpen ? PackIconKind.ArrowLeft : PackIconKind.ArrowRight;

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

    public MainWindowModel(IServiceProvider serviceProvider, IUserProfileService userProfileService, Core.Repositories.Login.IAuthService authService, IDialogService dialogService,
        ILoadingService loadingService) : base(dialogService, loadingService)
    {
        _serviceProvider = serviceProvider;
        _userProfileService = userProfileService;
        _authService = authService;
        ToggleMenuCommand = new RelayCommand(ToggleMenu);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenUserProfileCommand = new AsyncRelayCommand(OpenUserProfile);
        SignOutCommand = new RelayCommand(SignOut);
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
            new Shared.Entities.MenuItem { Text = "Configuración", IconKind = PackIconKind.Cog, ModuleName = "Configuracion" }
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
        Theme theme = paletteHelper.GetTheme();

        // Alternar entre el tema Dark y Light
        BaseTheme baseTheme = theme.GetBaseTheme() == BaseTheme.Dark ? BaseTheme.Light : BaseTheme.Dark;

        theme.SetBaseTheme(baseTheme);
        paletteHelper.SetTheme(theme);

    }

    public async Task LoadUserProfileAsync()
    {
        var profileResponse = await _userProfileService.GetUserProfileAsync();

        if (profileResponse?.Data != null)
        {
            UsuarioEmail = profileResponse.Data.GusUser ?? "No disponible";
            UsuarioNombre = profileResponse.Data.Gpe?.GpeNombre ?? "No disponible";
            UsuarioApellidos = profileResponse.Data.Gpe?.GpeApellidos ?? "";
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

    private async void SignOut()
    {
        try
        {
            await _authService.LogoutAsync();
        }
        catch { }

        try
        {
            var login = _serviceProvider.GetRequiredService<Views.Modulos.Login.Login>();
            login.Show();
        }
        catch { }

        Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.Close();
    }
}