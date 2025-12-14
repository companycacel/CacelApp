using CommunityToolkit.Mvvm.ComponentModel;

namespace CacelApp.Shared.Controls.WeightDisplay;

/// <summary>
/// Modelo de información para un display de balanza individual
/// Usado para binding dinámico en WeightDisplayControl
/// </summary>
public partial class BalanzaDisplayInfo : ObservableObject
{
    /// <summary>
    /// Nombre de la balanza (ej: "B1-A", "B2-A")
    /// </summary>
    [ObservableProperty]
    private string nombre = string.Empty;

    /// <summary>
    /// Puerto COM de la balanza (ej: "COM3", "COM4")
    /// </summary>
    [ObservableProperty]
    private string puerto = string.Empty;

    /// <summary>
    /// Peso actual leído de la balanza
    /// </summary>
    [ObservableProperty]
    private decimal? pesoActual;

    /// <summary>
    /// Indica si la balanza está conectada
    /// </summary>
    [ObservableProperty]
    private bool conectada;

    /// <summary>
    /// Color del borde del display (para diferenciar visualmente)
    /// Valores sugeridos: "#4F46E5" (índigo), "#10B981" (verde), "#F59E0B" (ámbar)
    /// </summary>
    [ObservableProperty]
    private string colorBorde = "#4F46E5";

    /// <summary>
    /// Comando para capturar el peso actual
    /// </summary>
    [ObservableProperty]
    private ICommand? capturarCommand;

    /// <summary>
    /// Indica si se debe mostrar el botón de captura
    /// </summary>
    [ObservableProperty]
    private bool mostrarBotonCaptura = true;
}
