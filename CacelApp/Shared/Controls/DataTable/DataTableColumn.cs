using System;
using MaterialDesignThemes.Wpf;

namespace CacelApp.Shared.Controls.DataTable;

/// <summary>
/// Define la configuración de una columna para el DataTable reutilizable
/// </summary>
public class DataTableColumn
{
    /// <summary>
    /// Nombre de la propiedad a mostrar (binding path)
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Texto del encabezado de la columna
    /// </summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>
    /// Ancho de la columna (puede ser * para proporcional, número fijo, o Auto)
    /// </summary>
    public string Width { get; set; } = "*";

    /// <summary>
    /// Formato de visualización (ej: "N2" para decimales, "C2" para moneda, "dd/MM/yyyy" para fechas)
    /// </summary>
    public string? StringFormat { get; set; }

    /// <summary>
    /// Alineación del contenido (Left, Center, Right)
    /// </summary>
    public string HorizontalAlignment { get; set; } = "Left";

    /// <summary>
    /// Si la columna es de solo lectura
    /// </summary>
    public bool IsReadOnly { get; set; } = true;

    /// <summary>
    /// Tipo de columna (Text, Template, CheckBox, etc.)
    /// </summary>
    public DataTableColumnType ColumnType { get; set; } = DataTableColumnType.Text;

    /// <summary>
    /// Propiedad del ícono para columnas con íconos
    /// </summary>
    public string? IconProperty { get; set; }

    /// <summary>
    /// Template personalizado (nombre del recurso o key)
    /// </summary>
    public string? TemplateKey { get; set; }

    /// <summary>
    /// Si la columna es ordenable
    /// </summary>
    public bool CanSort { get; set; } = true;

    /// <summary>
    /// Si se debe mostrar el total de esta columna
    /// </summary>
    public bool ShowTotal { get; set; } = false;

    /// <summary>
    /// Función de conversión personalizada
    /// </summary>
    public Func<object?, string>? CustomConverter { get; set; }

    /// <summary>
    /// Lista de botones de acción para columnas de tipo Actions
    /// </summary>
    public List<DataTableActionButton> ActionButtons { get; set; } = new();

    /// <summary>
    /// Comando para ejecutar cuando se hace click en un hipervínculo (para columnas tipo Hyperlink)
    /// </summary>
    public System.Windows.Input.ICommand? HyperlinkCommand { get; set; }

    /// <summary>
    /// Tooltip para hipervínculos
    /// </summary>
    public string? HyperlinkToolTip { get; set; }

    /// <summary>
    /// Prioridad de visualización (1 = siempre visible, 2 = ocultar en pantallas pequeñas, 3 = ocultar en pantallas medianas)
    /// </summary>
    public int DisplayPriority { get; set; } = 1;

    /// <summary>
    /// Si la columna se debe mostrar en la vista expandida cuando está oculta
    /// </summary>
    public bool ShowInExpandedView { get; set; } = true;

    /// <summary>
    /// Propiedad del binding para determinar si la columna es editable
    /// </summary>
    public string? IsEditableProperty { get; set; }

    /// <summary>
    /// Propiedad del binding para determinar si la columna es de solo lectura
    /// </summary>
    public string? IsReadOnlyProperty { get; set; }

    /// <summary>
    /// ItemsSource para columnas tipo ComboBox
    /// </summary>
    public object? ComboBoxItemsSource { get; set; }

    /// <summary>
    /// DisplayMemberPath para ComboBox
    /// </summary>
    public string? ComboBoxDisplayMemberPath { get; set; }

    /// <summary>
    /// SelectedValuePath para ComboBox
    /// </summary>
    public string? ComboBoxSelectedValuePath { get; set; }


    /// <summary>
    /// Indicador para manejo se Status (ícono y color) en columnas tipo Icon
    /// </summary>

    public StatusIndicator? Status { get; set; }

    /// <summary>
    /// Gets or sets the display variant for the cell content.
    /// </summary>
    public CellDisplayVariant Variant { get; set; } = CellDisplayVariant.Default;
    /// <summary>
    /// Gets or sets the color associated with the cell content.
    /// </summary>
    public string? Color { get; set; }
    /// <summary>
    /// Gets or sets the icon to display for the cell content.
    /// </summary>
    public PackIconKind Icon { get; set; } = PackIconKind.CloseCircleOutline;
}

public class StatusIndicator
{
    /// <summary>
    /// Ícono para mostrar cuando el valor booleano es true (para BooleanStatus)
    /// </summary>
    public PackIconKind BooleanTrueIcon { get; set; } = PackIconKind.CheckCircleOutline;

    /// <summary>
    /// Ícono para mostrar cuando el valor booleano es false (para BooleanStatus)
    /// </summary>
    public PackIconKind BooleanFalseIcon { get; set; } = PackIconKind.CloseCircleOutline;

    /// <summary>
    /// Color para el ícono cuando el valor es true (para BooleanStatus)
    /// </summary>
    public string? BooleanTrueColor { get; set; } = "#4CAF50";

    /// <summary>
    /// Color para el ícono cuando el valor es false (para BooleanStatus)
    /// </summary>
    public string? BooleanFalseColor { get; set; } = "#F44336";

    /// <summary>
    /// Texto del tooltip cuando el valor es true (para BooleanStatus)
    /// </summary>
    public string? BooleanTrueText { get; set; } = "Completado";

    /// <summary>
    /// Texto del tooltip cuando el valor es false (para BooleanStatus)
    /// </summary>
    public string? BooleanFalseText { get; set; } = "Pendiente";
}

/// <summary>
/// Tipos de columna soportados
/// </summary>
public enum DataTableColumnType
{
    Text,
    Number,
    Date,
    Currency,
    Boolean,
    BooleanStatus,  // Check verde / X roja para estados true/false
    Icon,
    Template,
    Actions,
    Hyperlink,
    EditableText,
    EditableNumber,
    ComboBox
}
public enum CellDisplayVariant
{
    Default,
    Filled,
    Outline,
    IconAndText
 
}