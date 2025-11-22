using CacelApp.Shared.Controls.DataTable;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CacelApp.Shared.Controls.DataTable;

/// <summary>
/// Control de tabla reutilizable con paginación, filtrado y columnas configurables
/// </summary>
public partial class DataTableControl : UserControl
{
    private double _currentWidth;
    private bool _isUpdatingColumns;
    private System.Windows.Threading.DispatcherTimer? _resizeTimer;

    public DataTableControl()
    {
        InitializeComponent();

        // Suscribirse al cambio de tamaño
        this.SizeChanged += DataTableControl_SizeChanged;
        this.Loaded += DataTableControl_Loaded;

        // Configurar timer para debouncing
        _resizeTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _resizeTimer.Tick += ResizeTimer_Tick;
    }

    /// <summary>
    /// Evento que se dispara cuando se intenta editar una celda
    /// Solo permite editar si IsEditing = true
    /// </summary>
    private void MainDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        // Obtener el item del DataContext de la fila
        if (e.Row.DataContext is IndexedItem<object> indexedItem)
        {
            // Verificar si el item tiene una propiedad IsEditing
            var itemType = indexedItem.Item.GetType();
            var isEditingProperty = itemType.GetProperty("IsEditing");

            if (isEditingProperty != null)
            {
                var isEditing = isEditingProperty.GetValue(indexedItem.Item);

                // Si IsEditing es false, cancelar la edición
                if (isEditing is bool editing && !editing)
                {
                    e.Cancel = true;
                }
            }
        }
    }

    private void DataTableControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Aplicar visibilidad inicial de columnas con el ancho real
        if (ActualWidth > 0)
        {
            _currentWidth = ActualWidth;
            UpdateColumnVisibility(ActualWidth);
        }
    }

    private void DataTableControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Usar debouncing para evitar múltiples actualizaciones durante redimensionamiento
        if (e.WidthChanged && Math.Abs(e.NewSize.Width - _currentWidth) > 1)
        {
            _currentWidth = e.NewSize.Width;

            // Detener timer anterior y reiniciar
            _resizeTimer?.Stop();
            _resizeTimer?.Start();
        }
    }

    private void ResizeTimer_Tick(object? sender, EventArgs e)
    {
        _resizeTimer?.Stop();

        // Usar Dispatcher para asegurar que el layout esté completo
        Dispatcher.InvokeAsync(() =>
        {
            UpdateColumnVisibility(_currentWidth);
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Actualiza la visibilidad de columnas según el ancho disponible
    /// </summary>
    private void UpdateColumnVisibility(double width)
    {
        if (Columns == null || MainDataGrid == null || MainDataGrid.Columns.Count == 0 || _isUpdatingColumns)
            return;

        _isUpdatingColumns = true;
        try
        {
            // Definir breakpoints
            bool isSmallScreen = width < 1000;
            bool isMediumScreen = width >= 1000 && width < 1400;
            bool hasHiddenColumns = false;

            // Verificar si hay columnas que pueden ocultarse (DisplayPriority > 1)
            bool hasExpandableColumns = Columns.Any(c => c.DisplayPriority > 1);

            // Primera pasada: calcular visibilidad sin considerar el expander aún
            int columnOffset = 1; // Solo N° por ahora

            for (int i = 0; i < Columns.Count; i++)
            {
                var config = Columns[i];
                int columnIndex = i + columnOffset;

                // Si hay expander, ajustar índice
                if (HasExpanderColumn())
                    columnIndex++;

                if (columnIndex >= MainDataGrid.Columns.Count)
                    break;

                var column = MainDataGrid.Columns[columnIndex];

                // Determinar si la columna debe estar visible
                bool shouldBeVisible = config.DisplayPriority switch
                {
                    1 => true, // Siempre visible
                    2 => !isSmallScreen, // Ocultar en pantallas pequeñas
                    3 => !isSmallScreen && !isMediumScreen, // Solo visible en pantallas grandes
                    _ => true
                };

                column.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;

                // Detectar si hay columnas ocultas (cualquiera con DisplayPriority > 1 que esté oculta)
                if (!shouldBeVisible && config.DisplayPriority > 1)
                {
                    hasHiddenColumns = true;
                }
            }

            // Gestionar el expander dinámicamente: agregar o remover según sea necesario
            bool currentlyHasExpander = HasExpanderColumn();
            bool shouldHaveExpander = hasExpandableColumns && hasHiddenColumns;

            if (shouldHaveExpander && !currentlyHasExpander)
            {
                // Agregar el expander
                var expanderConfig = new DataTableColumn
                {
                    PropertyName = "IsExpanded",
                    Header = "",
                    Width = "80",
                    ColumnType = DataTableColumnType.Template,
                    TemplateKey = "ExpanderTemplate",
                    CanSort = false,
                    DisplayPriority = 1,
                    ShowInExpandedView = false
                };

                var expanderColumn = CreateTemplateColumn(expanderConfig);
                expanderColumn.Header = expanderConfig.Header;
                expanderColumn.Width = ParseWidth(expanderConfig.Width);
                expanderColumn.CanUserSort = expanderConfig.CanSort;
                MainDataGrid.Columns.Insert(1, expanderColumn);

                // Forzar actualización del layout
                MainDataGrid.UpdateLayout();

                // Regenerar totales para incluir el espacio del expander
                GenerateTotalsRow();
            }
            else if (!shouldHaveExpander && currentlyHasExpander)
            {
                // Remover el expander
                MainDataGrid.Columns.RemoveAt(1);

                // Forzar actualización del layout
                MainDataGrid.UpdateLayout();

                // Regenerar totales para remover el espacio del expander
                GenerateTotalsRow();
            }
            else if (currentlyHasExpander)
            {
                // Solo actualizar visibilidad del expander si ya existe
                var expanderColumn = MainDataGrid.Columns[1];
                expanderColumn.Visibility = hasHiddenColumns ? Visibility.Visible : Visibility.Collapsed;
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
            if (!shouldHaveExpander || currentlyHasExpander == shouldHaveExpander)
            {
                // Solo actualizar si no acabamos de regenerar totales
                UpdateTotalsRowVisibility();
            }
        }
        finally
        {
            _isUpdatingColumns = false;
        }
    }

    /// <summary>
    /// Verifica si el DataGrid tiene una columna expander
    /// </summary>
    private bool HasExpanderColumn()
    {
        return MainDataGrid.Columns.Count > 1 &&
               MainDataGrid.Columns[1].Header?.ToString() == "" &&
               MainDataGrid.Columns[1].Width.Value == 80;
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
            control.ConfigureAutomaticFeatures();
        }
    }

    /// <summary>
    /// Configura automáticamente el filtro y los totales basándose en las columnas
    /// </summary>
    private void ConfigureAutomaticFeatures()
    {
        if (Columns == null || DataContext is not IDataTableViewModel viewModel)
            return;

        // 1. Configurar filtro automático para todas las columnas (excepto N°, Acciones, IsExpanded)
        var filterableColumns = Columns
            .Where(c => c.PropertyName != "IsExpanded"
                     && c.PropertyName != "Acciones"
                     && c.ColumnType != DataTableColumnType.Actions
                     && c.ColumnType != DataTableColumnType.Template)
            .ToList();

        if (filterableColumns.Any())
        {
            viewModel.CustomFilter = (item, searchTerm) =>
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return true;

                var term = searchTerm.ToLower();
                var itemProperty = item.GetType().GetProperty("Item");
                var actualItem = itemProperty?.GetValue(item) ?? item;

                foreach (var column in filterableColumns)
                {
                    try
                    {
                        var value = GetPropertyValueByPath(actualItem, column.PropertyName);
                        if (value != null)
                        {
                            var stringValue = value.ToString()?.ToLower();
                            if (!string.IsNullOrEmpty(stringValue) && stringValue.Contains(term))
                                return true;
                        }
                    }
                    catch
                    {
                        // Ignorar errores de reflexión
                    }
                }

                return false;
            };
        }

        // 2. Configurar totales automáticos para columnas con ShowTotal = true
        var totalColumns = Columns
            .Where(c => c.ShowTotal)
            .Select(c => c.PropertyName)
            .ToList();

        if (totalColumns.Any())
        {
            viewModel.ConfigureTotals(totalColumns);
        }
    }

    /// <summary>
    /// Obtiene el valor de una propiedad usando un path (ej: "Baz.baz_des")
    /// </summary>
    private object? GetPropertyValueByPath(object obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return null;

        var properties = propertyPath.Split('.');
        object? current = obj;

        foreach (var prop in properties)
        {
            if (current == null) return null;

            var propInfo = current.GetType().GetProperty(prop);
            if (propInfo == null) return null;

            current = propInfo.GetValue(current);
        }

        return current;
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
        var indexColumn = new System.Windows.Controls.DataGridTextColumn
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

        // Agregar columnas configuradas primero para determinar cuáles estarán ocultas
        foreach (var column in Columns.Where(c => c.PropertyName != "IsExpanded"))
        {
            DataGridColumn gridColumn = column.ColumnType switch
            {
                DataTableColumnType.Text => CreateTextColumn(column),
                DataTableColumnType.Number => CreateNumberColumn(column),
                DataTableColumnType.Date => CreateDateColumn(column),
                DataTableColumnType.Currency => CreateCurrencyColumn(column),
                DataTableColumnType.Boolean => CreateBooleanColumn(column),
                DataTableColumnType.BooleanStatus => CreateBooleanStatusColumn(column),
                DataTableColumnType.Hyperlink => CreateHyperlinkColumn(column),
                DataTableColumnType.Actions => CreateActionsColumn(column),
                DataTableColumnType.Template => CreateTemplateColumn(column),
                DataTableColumnType.EditableText => CreateEditableTextColumn(column),
                DataTableColumnType.EditableNumber => CreateEditableNumberColumn(column),
                DataTableColumnType.ComboBox => CreateComboBoxColumn(column),
                _ => CreateTextColumn(column)
            };

            gridColumn.Header = column.Header;
            gridColumn.Width = ParseWidth(column.Width);
            gridColumn.CanUserSort = column.CanSort;

            MainDataGrid.Columns.Add(gridColumn);
        }

        // Aplicar visibilidad inmediatamente de forma síncrona
        // Usar ActualWidth si está disponible, sino usar el ancho de la ventana padre
        double initialWidth = ActualWidth > 0 ? ActualWidth : 1920;
        if (initialWidth == 0)
        {
            var window = Window.GetWindow(this);
            if (window != null)
                initialWidth = window.ActualWidth;
        }

        UpdateColumnVisibility(initialWidth);
    }

    /// <summary>
    /// Aplica la visibilidad inicial a las columnas basándose en DisplayPriority y tamaño actual
    /// </summary>
    /// <summary>
    /// Verifica si hay columnas actualmente ocultas en el DataGrid
    /// </summary>
    private bool CheckHasHiddenColumns()
    {
        // Excluir la primera columna (N°) y verificar el resto
        for (int i = 1; i < MainDataGrid.Columns.Count; i++)
        {
            if (MainDataGrid.Columns[i].Visibility == Visibility.Collapsed)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Crea una columna de texto
    /// </summary>

    /// <summary>
    /// Crea una columna numérica
    /// </summary>
    private DataGridColumn CreateTextColumn(DataTableColumn config)
    {
        // Si no hay variante especial, usar columna de texto normal
        if (config.Variant == CellDisplayVariant.Default)
        {
            var column = new System.Windows.Controls.DataGridTextColumn
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

        // Si hay variante visual, usar DataGridTemplateColumn y el template adecuado
        var templateKey = config.Variant switch
        {
            CellDisplayVariant.Filled => "FilledStateTemplate",
            CellDisplayVariant.Outline => "OutlineStateTemplate",
            CellDisplayVariant.IconAndText => "IconStateTemplate",
            _ => null
        };

        var templateColumn = new DataGridTemplateColumn
        {
            IsReadOnly = config.IsReadOnly
        };

        if (!string.IsNullOrEmpty(templateKey))
        {
            // Buscar el template en los recursos
            var template = TryFindResource(templateKey) as DataTemplate;
            if (template != null)
            {
                // Primero setear el color
                ColumnMetadata.SetColor(templateColumn, config.Color);
                // Obtener el Brush actualizado
                var colorBrush = ColumnMetadata.GetColor(templateColumn);
                // Usar ContentPresenter y enlazar Content a un objeto con Value, Icon y Color
                var cellTemplate = new DataTemplate();
                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.ContentTemplateProperty, template);
                var multiBinding = new System.Windows.Data.MultiBinding { Mode = System.Windows.Data.BindingMode.OneWay };
                multiBinding.Bindings.Add(new System.Windows.Data.Binding($"Item.{config.PropertyName}"));
                multiBinding.Bindings.Add(new System.Windows.Data.Binding { Source = config.Icon });
                multiBinding.Bindings.Add(new System.Windows.Data.Binding { Source = colorBrush });
                multiBinding.Converter = new CellValueWithIconMultiConverter();
                contentPresenterFactory.SetBinding(ContentPresenter.ContentProperty, multiBinding);
                cellTemplate.VisualTree = contentPresenterFactory;
                templateColumn.CellTemplate = cellTemplate;
            }
        }
        return templateColumn;
    }

    // Permitir color e icono personalizados vía binding (si el template los soporta)
    /// <summary>
    /// Crea una columna numérica
    /// </summary>
    private System.Windows.Controls.DataGridTextColumn CreateNumberColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "N2";
        var column = new System.Windows.Controls.DataGridTextColumn
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
    /// Crea una columna de fecha
    /// </summary>
    private System.Windows.Controls.DataGridTextColumn CreateDateColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "dd/MM/yyyy";
        return new System.Windows.Controls.DataGridTextColumn
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
    private System.Windows.Controls.DataGridTextColumn CreateCurrencyColumn(DataTableColumn config)
    {
        var format = config.StringFormat ?? "C2";
        var column = new System.Windows.Controls.DataGridTextColumn
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
    /// Crea una columna con ícono de estado (check verde para true, X roja para false)
    /// Soporta expresiones complejas con acceso a propiedades del Item
    /// </summary>
    private DataGridTemplateColumn CreateBooleanStatusColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn
        {
            IsReadOnly = true
        };

        // Crear el template para el estado
        var factory = new FrameworkElementFactory(typeof(PackIcon));

        var status = config.Status ?? new StatusIndicator();

        // Binding al objeto completo Item para poder acceder a todas sus propiedades
        var kindParam = $"{status.BooleanTrueIcon}|{status.BooleanFalseIcon}";
        var kindBinding = new Binding("Item")
        {
            Converter = new ExpressionConverter(),
            ConverterParameter = new ExpressionParameter
            {
                PropertyName = config.PropertyName,
                Expression = kindParam,
                ReturnType = ExpressionReturnType.Icon
            }
        };
        factory.SetBinding(PackIcon.KindProperty, kindBinding);

        // Binding para el color del ícono
        var colorParam = $"{status.BooleanTrueColor ?? "#4CAF50"}|{status.BooleanFalseColor ?? "#F44336"}";
        var colorBinding = new Binding("Item")
        {
            Converter = new ExpressionConverter(),
            ConverterParameter = new ExpressionParameter
            {
                PropertyName = config.PropertyName,
                Expression = colorParam,
                ReturnType = ExpressionReturnType.Color
            }
        };
        factory.SetBinding(PackIcon.ForegroundProperty, colorBinding);

        // Propiedades del ícono
        factory.SetValue(PackIcon.WidthProperty, 24.0);
        factory.SetValue(PackIcon.HeightProperty, 24.0);
        factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Binding para el ToolTip
        var tooltipParam = $"{status.BooleanTrueText ?? "Completado"}|{status.BooleanFalseText ?? "Pendiente"}";
        var tooltipBinding = new Binding("Item")
        {
            Converter = new ExpressionConverter(),
            ConverterParameter = new ExpressionParameter
            {
                PropertyName = config.PropertyName,
                Expression = tooltipParam,
                ReturnType = ExpressionReturnType.Text
            }
        };
        factory.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);

        var dataTemplate = new DataTemplate
        {
            VisualTree = factory
        };

        column.CellTemplate = dataTemplate;
        return column;
    }

    /// <summary>
    /// Crea una columna de texto editable con modo de edición inline
    /// </summary>
    private DataGridTemplateColumn CreateEditableTextColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn();

        // Template único que muestra TextBlock o TextBox según IsEditing
        var template = new DataTemplate();
        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        gridFactory.SetValue(Grid.MarginProperty, new Thickness(5, 0, 5, 0));

        // TextBlock (modo lectura)
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"Item.{config.PropertyName}"));
        textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Trigger para ocultar cuando está editando
        var textBlockTrigger = new DataTrigger();
        textBlockTrigger.Binding = new Binding("Item.IsEditing");
        textBlockTrigger.Value = true;
        textBlockTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));

        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Triggers.Add(textBlockTrigger);
        textBlockFactory.SetValue(TextBlock.StyleProperty, textBlockStyle);

        // TextBox con estilo Material Design (modo edición)
        var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));

        // Aplicar estilo Material Design estándar
        var mdStyle = Application.Current.TryFindResource("MaterialDesignFilledTextBox") as Style;
        if (mdStyle != null)
        {
            textBoxFactory.SetValue(TextBox.StyleProperty, mdStyle);
        }

        textBoxFactory.SetBinding(TextBox.TextProperty, new Binding($"Item.{config.PropertyName}")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        textBoxFactory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBoxFactory.SetValue(TextBox.FontSizeProperty, 13.0);
        textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(8, 8, 8, 8));
        textBoxFactory.SetValue(TextBox.MarginProperty, new Thickness(0, 4, 0, 4));

        // Trigger para mostrar solo cuando está editando
        var textBoxTrigger = new DataTrigger();
        textBoxTrigger.Binding = new Binding("Item.IsEditing");
        textBoxTrigger.Value = false;
        textBoxTrigger.Setters.Add(new Setter(TextBox.VisibilityProperty, Visibility.Collapsed));

        var textBoxStyleWithTrigger = new Style(typeof(TextBox), mdStyle);
        textBoxStyleWithTrigger.Triggers.Add(textBoxTrigger);
        textBoxFactory.SetValue(TextBox.StyleProperty, textBoxStyleWithTrigger);

        gridFactory.AppendChild(textBlockFactory);
        gridFactory.AppendChild(textBoxFactory);

        template.VisualTree = gridFactory;
        column.CellTemplate = template;

        return column;
    }

    /// <summary>
    /// Crea una columna numérica editable con modo de edición inline
    /// </summary>
    private DataGridTemplateColumn CreateEditableNumberColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn();
        var format = config.StringFormat ?? "N2";

        // Template único que muestra TextBlock o TextBox según IsEditing
        var template = new DataTemplate();
        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        gridFactory.SetValue(Grid.MarginProperty, new Thickness(5, 0, 5, 0));

        // TextBlock (modo lectura)
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"Item.{config.PropertyName}")
        {
            StringFormat = $"{{0:{format}}}"
        });
        textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);

        // Trigger para ocultar cuando está editando
        var textBlockTrigger = new DataTrigger();
        textBlockTrigger.Binding = new Binding("Item.IsEditing");
        textBlockTrigger.Value = true;
        textBlockTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));

        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Triggers.Add(textBlockTrigger);
        textBlockFactory.SetValue(TextBlock.StyleProperty, textBlockStyle);

        // TextBox con estilo Material Design (modo edición)
        var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));

        // Aplicar estilo Material Design estándar
        var mdStyle = Application.Current.TryFindResource("MaterialDesignFilledTextBox") as Style;
        if (mdStyle != null)
        {
            textBoxFactory.SetValue(TextBox.StyleProperty, mdStyle);
        }

        textBoxFactory.SetBinding(TextBox.TextProperty, new Binding($"Item.{config.PropertyName}")
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        textBoxFactory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBoxFactory.SetValue(TextBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Right);
        textBoxFactory.SetValue(TextBox.FontSizeProperty, 13.0);
        textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(8, 8, 8, 8));
        textBoxFactory.SetValue(TextBox.MarginProperty, new Thickness(0, 4, 0, 4));

        // Binding de IsReadOnly para campos como Peso Bruto que pueden bloquearse
        if (config.PropertyName.Contains("Peso") || config.PropertyName.Contains("pb"))
        {
            textBoxFactory.SetBinding(TextBox.IsReadOnlyProperty, new Binding("Item.IsPesoBrutoReadOnly"));
        }

        // Trigger para mostrar solo cuando está editando
        var textBoxTrigger = new DataTrigger();
        textBoxTrigger.Binding = new Binding("Item.IsEditing");
        textBoxTrigger.Value = false;
        textBoxTrigger.Setters.Add(new Setter(TextBox.VisibilityProperty, Visibility.Collapsed));

        var textBoxStyleWithTrigger = new Style(typeof(TextBox), mdStyle);
        textBoxStyleWithTrigger.Triggers.Add(textBoxTrigger);
        textBoxFactory.SetValue(TextBox.StyleProperty, textBoxStyleWithTrigger);

        gridFactory.AppendChild(textBlockFactory);
        gridFactory.AppendChild(textBoxFactory);

        template.VisualTree = gridFactory;
        column.CellTemplate = template;

        return column;
    }

    /// <summary>
    /// Crea una columna con ComboBox para selección de opciones
    /// </summary>
    private DataGridTemplateColumn CreateComboBoxColumn(DataTableColumn config)
    {
        var column = new DataGridTemplateColumn();

        // Template único que muestra TextBlock o ComboBox según IsEditing
        var template = new DataTemplate();
        var gridFactory = new FrameworkElementFactory(typeof(Grid));
        gridFactory.SetValue(Grid.MarginProperty, new Thickness(5, 0, 5, 0));

        // TextBlock (modo lectura) - muestra el texto descriptivo
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
        var displayPropertyName = config.PropertyName.EndsWith("_id")
            ? config.PropertyName.Replace("_id", "_des")
            : config.PropertyName;

        textBlockFactory.SetBinding(TextBlock.TextProperty, new Binding($"Item.{displayPropertyName}"));
        textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

        // Trigger para ocultar cuando está editando
        var textBlockTrigger = new DataTrigger();
        textBlockTrigger.Binding = new Binding("Item.IsEditing");
        textBlockTrigger.Value = true;
        textBlockTrigger.Setters.Add(new Setter(TextBlock.VisibilityProperty, Visibility.Collapsed));

        var textBlockStyle = new Style(typeof(TextBlock));
        textBlockStyle.Triggers.Add(textBlockTrigger);
        textBlockFactory.SetValue(TextBlock.StyleProperty, textBlockStyle);

        // ComboBox con estilo Material Design (modo edición)
        var comboFactory = new FrameworkElementFactory(typeof(ComboBox));

        // Aplicar estilo Material Design estándar
        var mdStyle = Application.Current.TryFindResource("MaterialDesignFilledComboBox") as Style;
        if (mdStyle != null)
        {
            comboFactory.SetValue(ComboBox.StyleProperty, mdStyle);
        }

        // ItemsSource desde la colección proporcionada
        if (config.ComboBoxItemsSource != null)
        {
            comboFactory.SetValue(ComboBox.ItemsSourceProperty, config.ComboBoxItemsSource);
        }

        // DisplayMemberPath y SelectedValuePath para objetos complejos
        if (!string.IsNullOrEmpty(config.ComboBoxDisplayMemberPath))
        {
            comboFactory.SetValue(ComboBox.DisplayMemberPathProperty, config.ComboBoxDisplayMemberPath);
        }

        if (!string.IsNullOrEmpty(config.ComboBoxSelectedValuePath))
        {
            comboFactory.SetValue(ComboBox.SelectedValuePathProperty, config.ComboBoxSelectedValuePath);

            // Usar SelectedValue cuando hay SelectedValuePath
            comboFactory.SetBinding(ComboBox.SelectedValueProperty, new Binding($"Item.{config.PropertyName}")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }
        else
        {
            // Usar SelectedItem para colecciones simples (strings)
            comboFactory.SetBinding(ComboBox.SelectedItemProperty, new Binding($"Item.{config.PropertyName}")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }

        comboFactory.SetValue(ComboBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        comboFactory.SetValue(ComboBox.FontSizeProperty, 13.0);
        comboFactory.SetValue(ComboBox.PaddingProperty, new Thickness(8, 8, 8, 8));
        comboFactory.SetValue(ComboBox.MarginProperty, new Thickness(0, 4, 0, 4));

        // Trigger para mostrar solo cuando está editando
        var comboTrigger = new DataTrigger();
        comboTrigger.Binding = new Binding("Item.IsEditing");
        comboTrigger.Value = false;
        comboTrigger.Setters.Add(new Setter(ComboBox.VisibilityProperty, Visibility.Collapsed));

        var comboStyleWithTrigger = new Style(typeof(ComboBox), mdStyle);
        comboStyleWithTrigger.Triggers.Add(comboTrigger);
        comboFactory.SetValue(ComboBox.StyleProperty, comboStyleWithTrigger);

        gridFactory.AppendChild(textBlockFactory);
        gridFactory.AppendChild(comboFactory);

        template.VisualTree = gridFactory;
        column.CellTemplate = template;

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

        int columnIndex = 1;

        // Verificar si hay columnas ACTUALMENTE ocultas (verificar en el DataGrid)
        bool hasHiddenColumnsNow = HasExpanderColumn();
        if (hasHiddenColumnsNow)
        {
            // Agregar columna vacía para el expander
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            columnIndex++;
        }

        // Agregar columnas configuradas (excluyendo IsExpanded manual si existe)
        foreach (var column in Columns.Where(c => c.PropertyName != "IsExpanded"))
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

                // Binding al total de la columna usando indexador de diccionario
                var binding = new Binding($"ColumnTotals[{column.PropertyName}]")
                {
                    FallbackValue = "0"
                };

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

/// <summary>
/// Tipo de retorno para el ExpressionConverter
/// </summary>
public enum ExpressionReturnType
{
    Icon,
    Color,
    Text
}

/// <summary>
/// Parámetros para el ExpressionConverter
/// </summary>
public class ExpressionParameter
{
    public string PropertyName { get; set; } = "";
    public string Expression { get; set; } = "";
    public ExpressionReturnType ReturnType { get; set; }
}

/// <summary>
/// Convertidor potente que evalúa expresiones con acceso completo al objeto Item
/// Soporta:
/// - Formato simple: "Check|Close" (para booleanos)
/// - Expresiones condicionales: "Item.Estado == 1 ? Check : Item.Estado == 2 ? Alert : Close"
/// - Acceso a propiedades: "Item.PesoNeto", "Item.Cliente"
/// - Operaciones matemáticas: "Item.Bruto - Item.Tara"
/// - Comparaciones: ==, !=, >, <, >=, <=
/// </summary>
public class ExpressionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not ExpressionParameter expParam || value == null)
            return GetDefaultValue(targetType);

        try
        {
            var expression = expParam.Expression;
            if (string.IsNullOrWhiteSpace(expression))
                return GetDefaultValue(targetType);

            string result;

            // Formato simple "valor1|valor2" (para booleanos o valores convertibles)
            if (!expression.Contains('?') && expression.Contains('|'))
            {
                var propertyValue = GetPropertyValue(value, expParam.PropertyName);

                // Convertir el valor a booleano
                bool boolValue;
                if (propertyValue is bool b)
                {
                    boolValue = b;
                }
                else if (propertyValue is int intVal)
                {
                    boolValue = intVal == 1;
                }
                else if (propertyValue != null && propertyValue.GetType() == typeof(int?))
                {
                    var nullableInt = (int?)propertyValue;
                    boolValue = nullableInt == 1;
                }
                else
                {
                    return GetDefaultValue(targetType);
                }

                var parts = expression.Split('|');
                result = boolValue ? parts[0].Trim() : parts[1].Trim();
            }
            // Expresiones con operadores ternarios y acceso a propiedades
            else if (expression.Contains('?'))
            {
                result = EvaluateTernaryExpression(value, expression);
            }
            else
            {
                // Expresión simple o acceso a propiedad
                result = EvaluateExpression(value, expression);
            }

            // Convertir al tipo de destino según ReturnType
            return expParam.ReturnType switch
            {
                ExpressionReturnType.Icon => ConvertToIcon(result),
                ExpressionReturnType.Color => ConvertToColor(result),
                ExpressionReturnType.Text => result,
                _ => result
            };
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    /// <summary>
    /// Obtiene el valor de una propiedad del objeto usando reflexión
    /// Soporta: "PropertyName", "Item.PropertyName", propiedades anidadas
    /// </summary>
    private object? GetPropertyValue(object obj, string propertyPath)
    {
        try
        {
            // Remover "Item." si existe
            propertyPath = propertyPath.Replace("Item.", "").Trim();

            var properties = propertyPath.Split('.');
            object? current = obj;

            foreach (var prop in properties)
            {
                if (current == null) return null;

                var propInfo = current.GetType().GetProperty(prop);
                if (propInfo == null) return null;

                current = propInfo.GetValue(current);
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Evalúa expresiones simples o acceso a propiedades
    /// Ejemplos: "Item.Estado", "Item.Bruto - Item.Tara", "100"
    /// </summary>
    private string EvaluateExpression(object item, string expression)
    {
        expression = expression.Trim();

        // Si contiene "Item.", es acceso a propiedad
        if (expression.Contains("Item."))
        {
            // Operaciones matemáticas simples
            if (expression.Contains('+') || expression.Contains('-') ||
                expression.Contains('*') || expression.Contains('/'))
            {
                return EvaluateMathExpression(item, expression).ToString();
            }

            // Acceso simple a propiedad
            var value = GetPropertyValue(item, expression);
            return value?.ToString() ?? "";
        }

        return expression;
    }

    /// <summary>
    /// Evalúa expresiones matemáticas simples
    /// Ejemplo: "Item.Bruto - Item.Tara"
    /// </summary>
    private double EvaluateMathExpression(object item, string expression)
    {
        try
        {
            // Reemplazar referencias a propiedades con sus valores
            var tokens = expression.Split(new[] { '+', '-', '*', '/', '(', ')' },
                StringSplitOptions.RemoveEmptyEntries);

            var evalExpression = expression;
            foreach (var token in tokens)
            {
                var trimmedToken = token.Trim();
                if (trimmedToken.StartsWith("Item."))
                {
                    var value = GetPropertyValue(item, trimmedToken);
                    if (value != null)
                    {
                        evalExpression = evalExpression.Replace(trimmedToken, value.ToString());
                    }
                }
            }

            // Evaluar la expresión matemática (solo operaciones básicas)
            return EvaluateSimpleMath(evalExpression);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Evalúa operaciones matemáticas básicas sin usar eval dinámico
    /// </summary>
    private double EvaluateSimpleMath(string expression)
    {
        try
        {
            // Remover espacios
            expression = expression.Replace(" ", "");

            // Orden de operaciones: *, / primero, luego +, -
            // Esta es una implementación simple, para casos complejos usar NCalc o similar

            // Por ahora, solo suma/resta simple
            if (expression.Contains('+'))
            {
                var parts = expression.Split('+');
                return parts.Sum(p => double.Parse(p.Trim()));
            }
            if (expression.Contains('-'))
            {
                var parts = expression.Split('-');
                var result = double.Parse(parts[0].Trim());
                for (int i = 1; i < parts.Length; i++)
                    result -= double.Parse(parts[i].Trim());
                return result;
            }

            return double.Parse(expression);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Evalúa expresiones con operadores ternarios anidados
    /// Ejemplo: "Item.Estado == 1 ? Check : Item.Estado == 2 ? Alert : Close"
    /// </summary>
    private string EvaluateTernaryExpression(object item, string expression)
    {
        try
        {
            var questionIndex = expression.IndexOf('?');
            if (questionIndex == -1)
                return expression.Trim();

            var condition = expression.Substring(0, questionIndex).Trim();
            var colonIndex = FindMatchingColon(expression, questionIndex);

            if (colonIndex == -1)
                return expression.Trim();

            var trueValue = expression.Substring(questionIndex + 1, colonIndex - questionIndex - 1).Trim();
            var falseValue = expression.Substring(colonIndex + 1).Trim();

            bool conditionResult = EvaluateCondition(item, condition);

            string selectedBranch = conditionResult ? trueValue : falseValue;

            // Evaluar recursivamente si hay más ternarios
            if (selectedBranch.Contains('?'))
                return EvaluateTernaryExpression(item, selectedBranch);

            return selectedBranch;
        }
        catch
        {
            return expression.Trim();
        }
    }

    /// <summary>
    /// Encuentra el ':' que corresponde al '?' (manejando ternarios anidados)
    /// </summary>
    private int FindMatchingColon(string expression, int questionIndex)
    {
        int depth = 0;
        for (int i = questionIndex + 1; i < expression.Length; i++)
        {
            if (expression[i] == '?') depth++;
            else if (expression[i] == ':')
            {
                if (depth == 0) return i;
                depth--;
            }
        }
        return -1;
    }

    /// <summary>
    /// Evalúa condiciones con acceso a propiedades del Item
    /// Ejemplo: "Item.Estado == 1", "Item.PesoNeto > 1000"
    /// </summary>
    private bool EvaluateCondition(object item, string condition)
    {
        try
        {
            condition = condition.Trim();

            // Extraer operador
            string op = "";
            int opIndex = -1;

            foreach (var testOp in new[] { "==", "!=", ">=", "<=", ">", "<" })
            {
                opIndex = condition.IndexOf(testOp);
                if (opIndex != -1)
                {
                    op = testOp;
                    break;
                }
            }

            if (opIndex == -1)
            {
                // Sin operador, evaluar como booleano directo
                var value = EvaluateExpression(item, condition);
                return bool.TryParse(value, out bool boolResult) && boolResult;
            }

            // Dividir en partes izquierda y derecha
            var leftExpr = condition.Substring(0, opIndex).Trim();
            var rightExpr = condition.Substring(opIndex + op.Length).Trim();

            // Evaluar ambas partes
            var leftValue = EvaluateExpression(item, leftExpr);
            var rightValue = rightExpr.Trim('"', '\'');

            return CompareValues(leftValue, rightValue, op);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Compara dos valores usando el operador especificado
    /// </summary>
    private bool CompareValues(string leftValue, string rightValue, string op)
    {
        try
        {
            // Intentar comparación numérica
            if (double.TryParse(leftValue, out double numLeft) &&
                double.TryParse(rightValue, out double numRight))
            {
                return op switch
                {
                    "==" => Math.Abs(numLeft - numRight) < 0.0001,
                    "!=" => Math.Abs(numLeft - numRight) >= 0.0001,
                    ">" => numLeft > numRight,
                    "<" => numLeft < numRight,
                    ">=" => numLeft >= numRight,
                    "<=" => numLeft <= numRight,
                    _ => false
                };
            }

            // Comparación de strings
            return op switch
            {
                "==" => string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private object ConvertToIcon(string value)
    {
        return Enum.TryParse<PackIconKind>(value, true, out var icon)
            ? icon
            : PackIconKind.HelpCircle;
    }

    private object ConvertToColor(string value)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    private object GetDefaultValue(Type targetType)
    {
        return targetType.Name switch
        {
            nameof(PackIconKind) => PackIconKind.HelpCircle,
            nameof(Brush) or nameof(SolidColorBrush) => new SolidColorBrush(Colors.Gray),
            _ => "N/A"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
