using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace CacelApp.Shared.Entities;

public partial class BalanzaStatus : ObservableObject
{
    public string Name { get; set; }
    public string StatusText { get; set; }
    public bool IsOnline { get; set; }
    public PackIconKind IconKind { get; set; }

    [ObservableProperty]
    private decimal _currentWeight; // Peso actual (ej: 1230.50)

    // Propiedad formateada para el display (ej: "1,230.50 kg")
    public string DisplayWeight => $"{CurrentWeight:N2} kg";

    public bool IsWeightCaptured => CurrentWeight > 0;
}
