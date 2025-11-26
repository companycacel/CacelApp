using MaterialDesignThemes.Wpf;

namespace CacelApp.Shared.Controls.DataTable;

/// <summary>
/// Define un botón de acción para una columna de acciones en el DataTable
/// </summary>
public class DataTableActionButton
{
    // Constantes para dimensiones comunes
    public const double DefaultButtonWidth = 30;
    public const double DefaultButtonHeight = 30;
    public const double DefaultIconSize = 18;
    public const string DefaultMargin = "2,0";

    /// <summary>
    /// Icono del botón (Material Design)
    /// </summary>
    public PackIconKind Icon { get; set; }

    /// <summary>
    /// Texto del tooltip
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;

    /// <summary>
    /// Comando a ejecutar cuando se hace clic
    /// </summary>
    public ICommand? Command { get; set; }

    /// <summary>
    /// Color del botón (opcional, null para usar el color predeterminado)
    /// </summary>
    public System.Windows.Media.Brush? Foreground { get; set; }

    /// <summary>
    /// Ancho del botón
    /// </summary>
    public double Width { get; set; } = DefaultButtonWidth;

    /// <summary>
    /// Alto del botón
    /// </summary>
    public double Height { get; set; } = DefaultButtonHeight;

    /// <summary>
    /// Tamaño del icono
    /// </summary>
    public double IconSize { get; set; } = DefaultIconSize;

    /// <summary>
    /// Margen del botón
    /// </summary>
    public string Margin { get; set; } = DefaultMargin;

    /// <summary>
    /// Visibilidad condicional (función que determina si el botón debe mostrarse)
    /// </summary>
    public Func<object?, bool>? IsVisible { get; set; }
}
