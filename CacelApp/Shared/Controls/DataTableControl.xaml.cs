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
            control.GenerateTotalsRow();
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
                DataTableColumnType.Actions => CreateActionsColumn(column),
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
    /// Crea una columna con botones de acción configurables
    /// </summary>
    private DataGridTemplateColumn CreateActionsColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = true
        };

        // Crear el template dinámicamente
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
        factory.SetValue(StackPanel.HorizontalAlignmentProperty, 
            config.HorizontalAlignment == "Center" ? HorizontalAlignment.Center :
            config.HorizontalAlignment == "Right" ? HorizontalAlignment.Right :
            HorizontalAlignment.Left);

        // Agregar cada botón de acción
        foreach (var actionButton in config.ActionButtons)
        {
            var buttonFactory = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
            buttonFactory.SetValue(System.Windows.Controls.Button.StyleProperty, 
                Application.Current.TryFindResource("MaterialDesignIconButton"));
            buttonFactory.SetValue(System.Windows.Controls.Button.WidthProperty, actionButton.Width);
            buttonFactory.SetValue(System.Windows.Controls.Button.HeightProperty, actionButton.Height);
            buttonFactory.SetValue(System.Windows.Controls.Button.ToolTipProperty, actionButton.Tooltip);
            buttonFactory.SetValue(System.Windows.Controls.Button.MarginProperty, 
                ParseThickness(actionButton.Margin));

            // Establecer el comando
            if (actionButton.Command != null)
            {
                buttonFactory.SetValue(System.Windows.Controls.Button.CommandProperty, actionButton.Command);
                buttonFactory.SetBinding(System.Windows.Controls.Button.CommandParameterProperty, 
                    new Binding("Item"));
            }

            // Establecer el color si está especificado
            if (actionButton.Foreground != null)
            {
                buttonFactory.SetValue(System.Windows.Controls.Button.ForegroundProperty, actionButton.Foreground);
            }

            // Crear el icono
            var iconFactory = new FrameworkElementFactory(typeof(MaterialDesignThemes.Wpf.PackIcon));
            iconFactory.SetValue(MaterialDesignThemes.Wpf.PackIcon.KindProperty, actionButton.Icon);
            iconFactory.SetValue(MaterialDesignThemes.Wpf.PackIcon.WidthProperty, actionButton.IconSize);
            iconFactory.SetValue(MaterialDesignThemes.Wpf.PackIcon.HeightProperty, actionButton.IconSize);

            buttonFactory.AppendChild(iconFactory);
            factory.AppendChild(buttonFactory);
        }

        column.CellTemplate = new DataTemplate { VisualTree = factory };

        return column;
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

    /// <summary>
    /// Parsea un string de margen en Thickness
    /// </summary>
    private Thickness ParseThickness(string margin)
    {
        var parts = margin.Split(',');
        if (parts.Length == 1 && double.TryParse(parts[0], out var uniform))
            return new Thickness(uniform);
        if (parts.Length == 2 && double.TryParse(parts[0], out var horizontal) && double.TryParse(parts[1], out var vertical))
            return new Thickness(horizontal, vertical, horizontal, vertical);
        if (parts.Length == 4 && 
            double.TryParse(parts[0], out var left) && 
            double.TryParse(parts[1], out var top) && 
            double.TryParse(parts[2], out var right) && 
            double.TryParse(parts[3], out var bottom))
            return new Thickness(left, top, right, bottom);
        
        return new Thickness(0);
    }

    /// <summary>
    /// Genera la fila de totales dinámicamente
    /// </summary>
    private void GenerateTotalsRow()
    {
        if (Columns == null)
            return;

        // Buscar el Grid de totales
        var totalsGrid = this.FindName("TotalsGrid") as Grid;
        if (totalsGrid == null)
            return;

        totalsGrid.Children.Clear();
        totalsGrid.ColumnDefinitions.Clear();

        // Agregar columna para el índice (N°)
        totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        
        var indexLabel = new TextBlock
        {
            Text = "TOTALES",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(12, 0, 12, 0),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(33, 33, 33))
        };
        Grid.SetColumn(indexLabel, 0);
        totalsGrid.Children.Add(indexLabel);

        // Agregar columnas configuradas
        int columnIndex = 1;
        foreach (var column in Columns)
        {
            var colWidth = ParseWidth(column.Width);
            // Convertir DataGridLength a GridLength
            GridLength gridLength;
            if (colWidth.UnitType == DataGridLengthUnitType.Star)
                gridLength = new GridLength(colWidth.Value, GridUnitType.Star);
            else if (colWidth.IsAuto)
                gridLength = GridLength.Auto;
            else
                gridLength = new GridLength(colWidth.Value);

            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = gridLength });

            if (column.ShowTotal)
            {
                var totalBlock = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(12, 0, 12, 0),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(33, 33, 33))
                };

                // Binding al total de la columna
                var binding = new Binding($"ColumnTotals[{column.PropertyName}]");
                
                if (column.ColumnType == DataTableColumnType.Currency)
                {
                    binding.StringFormat = column.StringFormat ?? "C2";
                }
                else if (column.ColumnType == DataTableColumnType.Number)
                {
                    binding.StringFormat = column.StringFormat ?? "N2";
                }

                if (column.HorizontalAlignment == "Right")
                    totalBlock.HorizontalAlignment = HorizontalAlignment.Right;
                else if (column.HorizontalAlignment == "Center")
                    totalBlock.HorizontalAlignment = HorizontalAlignment.Center;

                totalBlock.SetBinding(TextBlock.TextProperty, binding);

                Grid.SetColumn(totalBlock, columnIndex);
                totalsGrid.Children.Add(totalBlock);
            }

            columnIndex++;
        }
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
