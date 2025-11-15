using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using System.Windows.Media;

namespace CacelApp.Shared.Controls;

/// <summary>
/// Define un botón de acción para una columna de acciones en el DataTable
/// </summary>
public class DataTableActionButton
{
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
    public Brush? Foreground { get; set; }

    /// <summary>
    /// Ancho del botón
    /// </summary>
    public double Width { get; set; } = 30;

    /// <summary>
    /// Alto del botón
    /// </summary>
    public double Height { get; set; } = 30;

    /// <summary>
    /// Tamaño del icono
    /// </summary>
    public double IconSize { get; set; } = 18;

    /// <summary>
    /// Margen del botón
    /// </summary>
    public string Margin { get; set; } = "2,0";

    /// <summary>
    /// Visibilidad condicional (función que determina si el botón debe mostrarse)
    /// </summary>
    public Func<object?, bool>? IsVisible { get; set; }
}
