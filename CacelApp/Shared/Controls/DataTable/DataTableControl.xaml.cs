using CacelApp.Shared.Controls.DataTable;
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
    private double _currentWidth;

    public DataTableControl()
    {
        InitializeComponent();
        
        // Suscribirse al cambio de tamaño
        this.SizeChanged += DataTableControl_SizeChanged;
        this.Loaded += DataTableControl_Loaded;
    }

    private void DataTableControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Aplicar visibilidad inicial de columnas
        UpdateColumnVisibility(this.ActualWidth);
    }

    private void DataTableControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Actualizar siempre que cambie el ancho
        if (e.WidthChanged)
        {
            _currentWidth = e.NewSize.Width;
            UpdateColumnVisibility(e.NewSize.Width);
        }
    }

    /// <summary>
    /// Actualiza la visibilidad de columnas según el ancho disponible
    /// </summary>
    private void UpdateColumnVisibility(double width)
    {
        if (Columns == null || MainDataGrid == null || MainDataGrid.Columns.Count == 0)
            return;

        // Definir breakpoints
        bool isSmallScreen = width < 1000;
        bool isMediumScreen = width >= 1000 && width < 1400;
        bool hasHiddenColumns = false;

        for (int i = 0; i < Columns.Count && i + 1 < MainDataGrid.Columns.Count; i++)
        {
            var config = Columns[i];
            var column = MainDataGrid.Columns[i + 1]; // +1 porque la primera es el índice

            // Determinar si la columna debe estar visible
            bool shouldBeVisible = config.DisplayPriority switch
            {
                1 => true, // Siempre visible
                2 => !isSmallScreen, // Ocultar en pantallas pequeñas
                3 => !isSmallScreen && !isMediumScreen, // Solo visible en pantallas grandes
                _ => true
            };

            column.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
            
            // Detectar si hay columnas ocultas (excepto el botón expansor mismo)
            if (!shouldBeVisible && config.ShowInExpandedView)
            {
                hasHiddenColumns = true;
            }
        }

        // Actualizar visibilidad del botón expansor (primera columna después del índice)
        if (Columns.Count > 0 && MainDataGrid.Columns.Count > 1)
        {
            // La columna del expansor es típicamente la segunda (índice 1)
            var expanderColumn = MainDataGrid.Columns[1];
            if (Columns[0].PropertyName == "IsExpanded")
            {
                expanderColumn.Visibility = hasHiddenColumns ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Forzar actualización de los detalles de fila cerrados si ya no hay columnas ocultas
        if (!hasHiddenColumns && DataContext is DataTableViewModel<object> viewModel)
        {
            foreach (var item in viewModel.PaginatedData)
            {
                if (item.IsExpanded)
                {
                    item.IsExpanded = false;
                }
            }
        }

        // Actualizar fila de totales para que coincida con las columnas visibles
        UpdateTotalsRowVisibility();
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
            control.SetupRowDetailsTemplate();
        }
    }

    /// <summary>
    /// Configura el template de detalles de fila con las columnas ocultas
    /// </summary>
    private void SetupRowDetailsTemplate()
    {
        if (Columns == null || MainDataGrid == null)
            return;

        // Crear el template dinámicamente
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, Application.Current.TryFindResource("MaterialDesignCardBackground"));
        factory.SetValue(Border.BorderBrushProperty, Application.Current.TryFindResource("MaterialDesignDivider"));
        factory.SetValue(Border.BorderThicknessProperty, new Thickness(0, 1, 0, 0));
        factory.SetValue(Border.PaddingProperty, new Thickness(60, 15, 15, 15));

        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        
        // Definir columnas del grid (Label y Value)
        var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(200));
        gridFactory.AppendChild(col1);
        
        var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
        col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        gridFactory.AppendChild(col2);

        // Filtrar columnas que deben mostrarse en la vista expandida
        // Excluir Actions y Template (como iconos de estado)
        var expandableColumns = Columns.Where(c => 
            c.ShowInExpandedView && 
            c.DisplayPriority > 1 && 
            c.ColumnType != DataTableColumnType.Actions &&
            c.ColumnType != DataTableColumnType.Template).ToList();

        int row = 0;
        foreach (var column in expandableColumns)
        {
            // Agregar definición de fila
            var rowDef = new FrameworkElementFactory(typeof(RowDefinition));
            rowDef.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            gridFactory.AppendChild(rowDef);

            // Label (Header)
            var labelFactory = new FrameworkElementFactory(typeof(TextBlock));
            labelFactory.SetValue(TextBlock.TextProperty, $"{column.Header}:");
            labelFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            labelFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.TryFindResource("MaterialDesignBody"));
            labelFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 5, 15, 5));
            labelFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            labelFactory.SetValue(Grid.RowProperty, row);
            labelFactory.SetValue(Grid.ColumnProperty, 0);
            gridFactory.AppendChild(labelFactory);

            // Value
            var valueFactory = CreateExpandedValueElement(column, row);
            gridFactory.AppendChild(valueFactory);

            row++;
        }

        factory.AppendChild(gridFactory);
        template.VisualTree = factory;
        MainDataGrid.RowDetailsTemplate = template;
    }

    /// <summary>
    /// Crea el elemento visual para el valor en la vista expandida
    /// </summary>
    private FrameworkElementFactory CreateExpandedValueElement(DataTableColumn column, int row)
    {
        FrameworkElementFactory valueFactory;

        switch (column.ColumnType)
        {
            case DataTableColumnType.Template:
                // Para templates, usar el mismo template
                if (!string.IsNullOrEmpty(column.TemplateKey))
                {
                    var contentControlFactory = new FrameworkElementFactory(typeof(ContentControl));
                    contentControlFactory.SetValue(ContentControl.ContentProperty, new Binding("Item"));
                    
                    var template = TryFindResource(column.TemplateKey) as DataTemplate;
                    if (template != null)
                    {
                        contentControlFactory.SetValue(ContentControl.ContentTemplateProperty, template);
                    }
                    
                    contentControlFactory.SetValue(Grid.RowProperty, row);
                    contentControlFactory.SetValue(Grid.ColumnProperty, 1);
                    return contentControlFactory;
                }
                goto default;

            default:
                // Para todos los demás tipos, usar TextBlock
                valueFactory = new FrameworkElementFactory(typeof(TextBlock));
                
                var binding = new Binding($"Item.{column.PropertyName}");
                
                // Aplicar formato según el tipo
                if (!string.IsNullOrEmpty(column.StringFormat))
                {
                    binding.StringFormat = column.ColumnType == DataTableColumnType.Currency
                        ? $"{{0:{column.StringFormat}}}"
                        : column.StringFormat;
                }
                else if (column.ColumnType == DataTableColumnType.Currency)
                {
                    binding.StringFormat = "{0:C2}";
                }
                else if (column.ColumnType == DataTableColumnType.Number)
                {
                    binding.StringFormat = "{0:N2}";
                }
                
                valueFactory.SetBinding(TextBlock.TextProperty, binding);
                valueFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.TryFindResource("MaterialDesignBodyLight"));
                valueFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 5, 0, 5));
                valueFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                valueFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                valueFactory.SetValue(Grid.RowProperty, row);
                valueFactory.SetValue(Grid.ColumnProperty, 1);
                
                break;
        }

        return valueFactory;
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
                DataTableColumnType.Hyperlink => CreateHyperlinkColumn(column),
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
    /// Crea una columna con hipervínculo clickeable
    /// </summary>
    private DataGridTemplateColumn CreateHyperlinkColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = true
        };

        // Crear el template para el hipervínculo
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        
        // Crear el Hyperlink interno
        var hyperlinkFactory = new FrameworkElementFactory(typeof(System.Windows.Documents.Hyperlink));
        hyperlinkFactory.SetBinding(
            System.Windows.Documents.Hyperlink.CommandProperty,
            new Binding
            {
                Source = config.HyperlinkCommand
            });
        hyperlinkFactory.SetBinding(
            System.Windows.Documents.Hyperlink.CommandParameterProperty,
            new Binding("Item"));
        
        if (!string.IsNullOrEmpty(config.HyperlinkToolTip))
        {
            hyperlinkFactory.SetValue(
                System.Windows.Documents.Hyperlink.ToolTipProperty,
                config.HyperlinkToolTip);
        }

        // Crear el Run para el texto del hipervínculo
        var runFactory = new FrameworkElementFactory(typeof(System.Windows.Documents.Run));
        runFactory.SetBinding(
            System.Windows.Documents.Run.TextProperty,
            new Binding($"Item.{config.PropertyName}"));

        hyperlinkFactory.AppendChild(runFactory);
        factory.AppendChild(hyperlinkFactory);

        column.CellTemplate = new DataTemplate { VisualTree = factory };

        return column;
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
        factory.SetValue(StackPanel.HorizontalAlignmentProperty, ParseHorizontalAlignment(config.HorizontalAlignment));

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
    /// Parsea un string de alineación horizontal
    /// </summary>
    private static HorizontalAlignment ParseHorizontalAlignment(string alignment)
    {
        return alignment switch
        {
            "Center" => HorizontalAlignment.Center,
            "Right" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };
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
                        System.Windows.Media.Color.FromRgb(33, 33, 33)),
                    // Agregar Tag para identificar la columna asociada
                    Tag = column.PropertyName
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

    /// <summary>
    /// Actualiza la visibilidad de las celdas de totales según las columnas visibles
    /// </summary>
    private void UpdateTotalsRowVisibility()
    {
        var totalsGrid = this.FindName("TotalsGrid") as Grid;
        if (totalsGrid == null || Columns == null)
            return;

        // Iterar sobre las columnas del DataGrid y sincronizar con los totales
        for (int i = 0; i < Columns.Count && i + 1 < MainDataGrid.Columns.Count; i++)
        {
            var config = Columns[i];
            var dataGridColumn = MainDataGrid.Columns[i + 1]; // +1 por columna de índice
            
            // Encontrar el TextBlock correspondiente en totalsGrid por Tag
            foreach (var child in totalsGrid.Children)
            {
                if (child is TextBlock textBlock && textBlock.Tag?.ToString() == config.PropertyName)
                {
                    // Sincronizar visibilidad con la columna del DataGrid
                    textBlock.Visibility = dataGridColumn.Visibility;
                    break;
                }
            }
        }
    }
}/// <summary>
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
