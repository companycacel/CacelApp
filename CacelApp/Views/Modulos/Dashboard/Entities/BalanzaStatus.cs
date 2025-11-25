using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace CacelApp.Shared.Entities;

public partial class BalanzaStatus : ObservableObject
{
    public string Name { get; set; }
    public string StatusText { get; set; }
    public bool IsOnline { get; set; }
    public PackIconKind IconKind { get; set; }
    public string Puerto { get; set; } // Puerto COM (ej: "COM6")
    public List<int> Camaras { get; set; } = new(); // Cámaras asociadas (ej: [1, 2])

    [ObservableProperty]
    private decimal _currentWeight; // Peso actual (ej: 1230.50)

    // Propiedad formateada para el display (ej: "1,230.50 kg")
    public string DisplayWeight => $"{CurrentWeight:N2} kg";

    public bool IsWeightCaptured => CurrentWeight > 0;
    
    // Propiedad para mostrar cámaras como texto
    public string CamarasText => Camaras.Any() ? $"Cámaras: {string.Join(", ", Camaras)}" : "Sin cámaras";
    
    // Método para notificar cambios en propiedades calculadas
    partial void OnCurrentWeightChanged(decimal value)
    {
        OnPropertyChanged(nameof(DisplayWeight));
        OnPropertyChanged(nameof(IsWeightCaptured));
    }
}
