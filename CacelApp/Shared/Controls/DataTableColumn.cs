using System;

namespace CacelApp.Shared.Controls;

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
    Icon,
    Template,
    Actions,
    Hyperlink
}
