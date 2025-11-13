using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;

namespace CacelApp.Shared.Entities;

public enum AlertType { Info, Warning, Error, Success }

public class DialogConfig
{
    public string Title { get; set; }
    public string Message { get; set; }
    public AlertType Type { get; set; }
    public PackIconKind IconKind { get; set; }
    public Brush AccentColor { get; set; }

    public IRelayCommand PrimaryCommand { get; set; } // Botón Continuar/Aceptar
    public string PrimaryText { get; set; } = "Aceptar";

    public IRelayCommand SecondaryCommand { get; set; } // Botón Cancelar
    public string SecondaryText { get; set; } = null; // Opcional
}
