using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CacelApp;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private bool _isMenuOpen = true;

    [ObservableProperty]
    private UserControl _currentView; 

    [ObservableProperty]
    private string _currentModuleTitle = "Inicio"; 

    // Constructor con Inyección de Dependencias
    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ToggleMenuCommand = new RelayCommand(ToggleMenu);
        NavigateCommand = new RelayCommand<string>(Navigate);

        // Cargar la vista por defecto al iniciar (e.g., BalanzaView)
        Navigate("Balanza");
    }

    public ICommand ToggleMenuCommand { get; }
    public ICommand NavigateCommand { get; }

    private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    private void Navigate(string moduleName)
    {
        // Usamos IServiceProvider para resolver la Vista del módulo
        switch (moduleName)
        {
            case "Balanza":
                //CurrentView = _serviceProvider.GetRequiredService<Modulos.Balanza.BalanzaView>();
                CurrentModuleTitle = "Gestión de Balanza";
                break;
            case "Pesajes":
                // Asume que también crearás PesajesView
                // CurrentView = _serviceProvider.GetRequiredService<Modulos.Pesajes.PesajesView>();
                CurrentModuleTitle = "Documentos y Pesajes";
                break;
            // ... otros módulos
            default:
                //CurrentView = new TextBlock { Text = $"Módulo {moduleName} no implementado.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                CurrentModuleTitle = moduleName;
                break;
        }

        IsMenuOpen = false;
    }
}