using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using CacelApp.Shared.Controls.Form;

namespace CacelApp.Shared.Controls.DataTable;

/// <summary>
/// Define un botón de acción para el header del DataTable
/// </summary>
public class HeaderActionDef
{
    /// <summary>
    /// Texto del botón
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Icono de Material Design
    /// </summary>
    public PackIconKind Icon { get; set; }

    /// <summary>
    /// Comando a ejecutar
    /// </summary>
    public ICommand Command { get; set; } = null!;

    /// <summary>
    /// Tooltip
    /// </summary>
    public string? Tooltip { get; set; }

    /// <summary>
    /// Variante del botón (Primary, Success, Warning, Danger, Custom, etc.)
    /// </summary>
    public ButtonVariant Variant { get; set; } = ButtonVariant.Custom;

    /// <summary>
    /// Si es true, usa estilo Outlined; si es false, usa estilo Filled/Raised
    /// </summary>
    public bool IsOutlined { get; set; } = false;

    /// <summary>
    /// Color de fondo personalizado (solo para Variant = Custom)
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Altura del botón
    /// </summary>
    public double Height { get; set; } = 36;

    /// <summary>
    /// Margen del botón
    /// </summary>
    public string Margin { get; set; } = "0,0,8,0";

    /// <summary>
    /// Función que determina si el botón está deshabilitado
    /// </summary>
    public Func<bool>? IsDisabled { get; set; }
}
