using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CacelApp.Shared.Controls;

/// <summary>
/// Control de tabla reutilizable con paginación, filtrado y columnas configurables
/// </summary>
public partial class DataTableControl : UserControl
{
    public DataTableControl()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>
    /// Colección de columnas a mostrar
    /// </summary>
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(
            nameof(Columns),
            typeof(ObservableCollection<DataTableColumn>),
            typeof(DataTableControl),
            new PropertyMetadata(null, OnColumnsChanged));

    public ObservableCollection<DataTableColumn> Columns
    {
        get => (ObservableCollection<DataTableColumn>)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// ItemsSource para los datos
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(System.Collections.IEnumerable),
            typeof(DataTableControl),
            new PropertyMetadata(null));

    public System.Collections.IEnumerable ItemsSource
    {
        get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Item seleccionado
    /// </summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(DataTableControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    /// <summary>
    /// Callback cuando cambian las columnas
    /// </summary>
    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl control)
        {
            control.GenerateColumns();
        }
    }

    /// <summary>
    /// Genera las columnas del DataGrid basándose en la configuración
    /// </summary>
    private void GenerateColumns()
    {
        if (Columns == null || MainDataGrid == null)
            return;

        MainDataGrid.Columns.Clear();

        // Agregar columna de índice automática al inicio
        var indexColumn = new DataGridTextColumn
        {
            Header = "N°",
            Width = new DataGridLength(60),
            IsReadOnly = true,
            Binding = new Binding("RowNumber")
        };

        var indexStyle = new Style(typeof(TextBlock));
        indexStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
        indexStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        indexColumn.ElementStyle = indexStyle;

        MainDataGrid.Columns.Add(indexColumn);

        // Agregar columnas configuradas
        foreach (var column in Columns)
        {
            DataGridColumn gridColumn = column.ColumnType switch
            {
                DataTableColumnType.Text => CreateTextColumn(column),
                DataTableColumnType.Number => CreateNumberColumn(column),
                DataTableColumnType.Date => CreateDateColumn(column),
                DataTableColumnType.Currency => CreateCurrencyColumn(column),
                DataTableColumnType.Boolean => CreateBooleanColumn(column),
                DataTableColumnType.Template => CreateTemplateColumn(column),
                _ => CreateTextColumn(column)
            };

            gridColumn.Header = column.Header;
            gridColumn.Width = ParseWidth(column.Width);
            gridColumn.CanUserSort = column.CanSort;

            MainDataGrid.Columns.Add(gridColumn);
        }
    }

    /// <summary>
    /// Crea una columna de texto
    /// </summary>
    private DataGridTextColumn CreateTextColumn(DataTableColumn config)
    {
        var column = new DataGridTextColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}")
            {
                StringFormat = config.StringFormat
            },
            IsReadOnly = config.IsReadOnly
        };

        if (config.HorizontalAlignment != "Left")
        {
            column.ElementStyle = CreateTextBlockStyle(config.HorizontalAlignment);
        }

        return column;
    }

    /// <summary>
    /// Crea una columna numérica
    /// </summary>
    private DataGridTextColumn CreateNumberColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "N2";
        var column = new DataGridTextColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}")
            {
                StringFormat = $"{{0:{format}}}"
            },
            IsReadOnly = config.IsReadOnly
        };

        column.ElementStyle = CreateTextBlockStyle(config.HorizontalAlignment == "Left" ? "Right" : config.HorizontalAlignment);

        return column;
    }

    /// <summary>
    /// Crea una columna de fecha
    /// </summary>
    private DataGridTextColumn CreateDateColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "dd/MM/yyyy";
        return new DataGridTextColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}")
            {
                StringFormat = $"{{0:{format}}}"
            },
            IsReadOnly = config.IsReadOnly
        };
    }

    /// <summary>
    /// Crea una columna de moneda
    /// </summary>
    private DataGridTextColumn CreateCurrencyColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "C2";
        var column = new DataGridTextColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}")
            {
                StringFormat = $"{{0:{format}}}"
            },
            IsReadOnly = config.IsReadOnly
        };

        column.ElementStyle = CreateTextBlockStyle("Right");

        return column;
    }

    /// <summary>
    /// Crea una columna de checkbox
    /// </summary>
    private DataGridCheckBoxColumn CreateBooleanColumn(DataTableColumn config)
    {
        return new DataGridCheckBoxColumn
        {
            Binding = new Binding($"Item.{config.PropertyName}"),
            IsReadOnly = config.IsReadOnly
        };
    }

    /// <summary>
    /// Crea una columna con template personalizado
    /// </summary>
    private DataGridTemplateColumn CreateTemplateColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = config.IsReadOnly
        };

        if (!string.IsNullOrEmpty(config.TemplateKey))
        {
            // Intentar buscar en los recursos de este control
            DataTemplate? template = TryFindResource(config.TemplateKey) as DataTemplate;
            
            // Si no se encuentra, buscar en el Application.Current.Resources
            if (template == null && Application.Current != null)
            {
                template = Application.Current.TryFindResource(config.TemplateKey) as DataTemplate;
            }

            // Si aún no se encuentra, buscar en el árbol visual hacia arriba
            if (template == null)
            {
                DependencyObject? parent = this;
                while (parent != null && template == null)
                {
                    parent = LogicalTreeHelper.GetParent(parent);
                    if (parent is FrameworkElement fe)
                    {
                        template = fe.TryFindResource(config.TemplateKey) as DataTemplate;
                    }
                }
            }

            if (template != null)
            {
                column.CellTemplate = template;
            }
        }

        return column;
    }

    /// <summary>
    /// Crea un estilo para TextBlock con alineación específica
    /// </summary>
    private Style CreateTextBlockStyle(string alignment)
    {
        var style = new Style(typeof(TextBlock));
        
        var horizontalAlignment = alignment switch
        {
            "Center" => HorizontalAlignment.Center,
            "Right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };

        style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, horizontalAlignment));
        style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(5, 0, 5, 0)));

        return style;
    }

    /// <summary>
    /// Parsea el ancho de columna
    /// </summary>
    private DataGridLength ParseWidth(string width)
    {
        if (width == "*")
            return new DataGridLength(1, DataGridLengthUnitType.Star);

        if (width.EndsWith("*"))
        {
            var value = width.TrimEnd('*');
            if (double.TryParse(value, out var starValue))
                return new DataGridLength(starValue, DataGridLengthUnitType.Star);
        }

        if (width.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return DataGridLength.Auto;

        if (double.TryParse(width, out var pixels))
            return new DataGridLength(pixels);

        return new DataGridLength(1, DataGridLengthUnitType.Star);
    }
}

/// <summary>
/// Convertidor para mostrar el índice de fila (base 1) con offset de página
/// </summary>
public class IndexConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Verificar que los valores no sean nulos ni UnsetValue
        if (values.Length >= 2 && 
            values[0] != DependencyProperty.UnsetValue && 
            values[1] != DependencyProperty.UnsetValue)
        {
            int rowIndex = 0;
            int pageStartIndex = 0;
            
            if (values[0] is int idx)
                rowIndex = idx;
            
            if (values[1] is int offset)
                pageStartIndex = offset;
                
            // rowIndex es el AlternationIndex (0-based dentro de la página)
            // pageStartIndex es el offset de registros de páginas anteriores
            return (pageStartIndex + rowIndex + 1).ToString();
        }
        return "0";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
