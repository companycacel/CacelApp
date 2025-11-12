using CacelApp.Views.Modulos.Dashboard;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace CacelApp;

public partial class MainWindowModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isMenuOpen = true;
    public double MenuWidth => IsMenuOpen ? 220 : 60;
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
    private string _usuarioEmail = "admin@cacel.com";
    public ICommand ToggleMenuCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand OpenUserProfileCommand { get; }
    public MainWindowModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ToggleMenuCommand = new RelayCommand(ToggleMenu);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        OpenUserProfileCommand = new RelayCommand(OpenUserProfile);

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
            // Agrega el resto de los módulos aquí (Balanza, Pesajes, Produccion, Configuracion)
            // Ejemplo:
            // "Balanza" => _serviceProvider.GetRequiredService<Modulos.Balanza.BalanzaView>(),
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

    private void OpenUserProfile()
    {
        // Lógica para abrir la ventana/diálogo de perfil de usuario
        MessageBox.Show($"Abriendo perfil de {UsuarioEmail}");
    }
}