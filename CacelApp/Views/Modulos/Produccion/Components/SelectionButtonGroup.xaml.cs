using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Core.Shared.Entities;

using TextBox = global::System.Windows.Controls.TextBox;
using KeyEventArgs = global::System.Windows.Input.KeyEventArgs;
using Key = global::System.Windows.Input.Key;

namespace CacelApp.Views.Modulos.Produccion.Components
{
    public partial class SelectionButtonGroup : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public SelectionButtonGroup()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
            Focusable = true;
            FocusVisualStyle = null;
        }

        #region Dependency Properties

        // ItemsSource
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(SelectionButtonGroup), 
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // SelectedValue (TwoWay)
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(object), typeof(SelectionButtonGroup), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

        public object SelectedValue
        {
            get { return GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }

        // GroupName
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register("GroupName", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata(Guid.NewGuid().ToString()));

        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNameProperty, value); }
        }

        // AutoOpenSearchOnFocus
        public static readonly DependencyProperty AutoOpenSearchOnFocusProperty =
            DependencyProperty.Register("AutoOpenSearchOnFocus", typeof(bool), typeof(SelectionButtonGroup), new PropertyMetadata(false));

        public bool AutoOpenSearchOnFocus
        {
            get { return (bool)GetValue(AutoOpenSearchOnFocusProperty); }
            set { SetValue(AutoOpenSearchOnFocusProperty, value); }
        }

        // Title
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata(string.Empty));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // CodeMemberPath
        public static readonly DependencyProperty CodeMemberPathProperty =
            DependencyProperty.Register("CodeMemberPath", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata("Ext.bie_codigo"));

        public string CodeMemberPath
        {
            get { return (string)GetValue(CodeMemberPathProperty); }
            set { SetValue(CodeMemberPathProperty, value); }
        }


        // IsSearchInlineEnabled
        public static readonly DependencyProperty IsSearchInlineEnabledProperty =
            DependencyProperty.Register("IsSearchInlineEnabled", typeof(bool), typeof(SelectionButtonGroup), new PropertyMetadata(false));

        public bool IsSearchInlineEnabled
        {
            get { return (bool)GetValue(IsSearchInlineEnabledProperty); }
            set { SetValue(IsSearchInlineEnabledProperty, value); }
        }

        // IsComboBoxVisible
        public static readonly DependencyProperty IsComboBoxVisibleProperty =
            DependencyProperty.Register("IsComboBoxVisible", typeof(bool), typeof(SelectionButtonGroup), new PropertyMetadata(false));

        public bool IsComboBoxVisible
        {
            get { return (bool)GetValue(IsComboBoxVisibleProperty); }
            set { SetValue(IsComboBoxVisibleProperty, value); }
        }

        // SearchLabel
        public static readonly DependencyProperty SearchLabelProperty =
            DependencyProperty.Register("SearchLabel", typeof(string), typeof(SelectionButtonGroup), new PropertyMetadata("Seleccionar o escribir..."));

        public string SearchLabel
        {
            get { return (string)GetValue(SearchLabelProperty); }
            set { SetValue(SearchLabelProperty, value); }
        }

        // ExtData (Para el ComboBox interno)
        public static readonly DependencyProperty ExtDataProperty =
            DependencyProperty.Register("ExtData", typeof(object), typeof(SelectionButtonGroup), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object ExtData
        {
            get { return GetValue(ExtDataProperty); }
            set { SetValue(ExtDataProperty, value); }
        }



        // DisplayMode
        public enum DisplayModeEnum { Single, Double }
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register("DisplayMode", typeof(DisplayModeEnum), typeof(SelectionButtonGroup), new PropertyMetadata(DisplayModeEnum.Single));

        public DisplayModeEnum DisplayMode
        {
            get { return (DisplayModeEnum)GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }

        // ItemsPerPage
        public static readonly DependencyProperty ItemsPerPageProperty =
            DependencyProperty.Register("ItemsPerPage", typeof(int), typeof(SelectionButtonGroup), 
                new PropertyMetadata(0, OnPaginationConfigChanged));

        public int ItemsPerPage
        {
            get { return (int)GetValue(ItemsPerPageProperty); }
            set { SetValue(ItemsPerPageProperty, value); }
        }

        // Rows
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register("Rows", typeof(int), typeof(SelectionButtonGroup), new PropertyMetadata(1));

        public int Rows
        {
            get { return (int)GetValue(RowsProperty); }
            set { SetValue(RowsProperty, value); }
        }

        // Columns
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register("Columns", typeof(int), typeof(SelectionButtonGroup), new PropertyMetadata(1));

        public int Columns
        {
            get { return (int)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        // IsSearchEnabled
        public static readonly DependencyProperty IsSearchEnabledProperty =
            DependencyProperty.Register("IsSearchEnabled", typeof(bool), typeof(SelectionButtonGroup), new PropertyMetadata(true));

        public bool IsSearchEnabled
        {
            get { return (bool)GetValue(IsSearchEnabledProperty); }
            set { SetValue(IsSearchEnabledProperty, value); }
        }
        
        // Evento Checked
        public static readonly RoutedEvent CheckedEvent = EventManager.RegisterRoutedEvent(
            "Checked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectionButtonGroup));

        public event RoutedEventHandler Checked
        {
            add { AddHandler(CheckedEvent, value); }
            remove { RemoveHandler(CheckedEvent, value); }
        }

        #endregion

        #region Internal Properties & Logic

        public ObservableCollection<object> PagedItems { get; private set; } = new ObservableCollection<object>();
        private bool _isClosingSearch;

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                    if (FilteredItems != null) UpdatePagedItemsFromFiltered();
                    else UpdatePagedItems();
                }
            }
        }

        private int _totalPages = 1;
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (_totalPages != value)
                {
                    _totalPages = value;
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(ShowPagination));
                    // Si no es DP, tendríamos que notificar, pero ahora es DP. 
                    // Sin embargo, si queremos que se auto-calcule, debemos manejarlo aquí.
                }
            }
        }

        public bool ShowPagination => ItemsPerPage > 0 && TotalPages > 1;

        // Propiedad calculada para el estilo actual del botón
        private Style _currentButtonStyle;
        public Style CurrentButtonStyle
        {
            get => _currentButtonStyle;
            set
            {
                _currentButtonStyle = value;
                OnPropertyChanged(nameof(CurrentButtonStyle));
            }
        }

        #endregion

        #region Commands

        private ICommand _nextPageCommand;
        public ICommand NextPageCommand => _nextPageCommand ??= new RelayCommand(() =>
        {
            if (CurrentPage < TotalPages) CurrentPage++;
        });

        private ICommand _previousPageCommand => _previousPageCommandInternal ??= new RelayCommand(() =>
        {
            if (CurrentPage > 1) CurrentPage--;
        });
        private ICommand _previousPageCommandInternal;
        public ICommand PreviousPageCommand => _previousPageCommand;

        #endregion

        #region Event Handlers

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SelectionButtonGroup)d;
            if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged collection)
            {
                // Si la colección cambia, reiniciamos paginación
                // Nota: Para implementación completa deberíamos escuchar CollectionChanged
                // Por simplicidad, asumimos que si cambia la referencia o contenido inicial, recargamos.
                // Si la colección ItemsSource se modifica dinámicamente, habría que suscribirse al evento.
                // Para este caso de uso MVVM donde se reemplaza o limpia/llena, esto basta por ahora 
                // si se dispara PropertyChanged del ItemsSource. 
                // Si ItemsSource es ObservableCollection y solo haces Add/Remove, necesitamos suscripción.
                // Vamos a suscribirnos simple:
               
            }
            control.CurrentPage = 1;
            control.ApplyFilter(control.InlineSearchText); // Usar ApplyFilter en lugar de cálculo directo
            
            // Sincronizar página si hay algo seleccionado
            if (control.SelectedValue != null) control.SyncPageWithSelection(control.SelectedValue);

            // Hack para suscribirse a cambios en la colección
            if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= control.OnSourceCollectionChanged;
            
            if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                newColl.CollectionChanged += control.OnSourceCollectionChanged;
        }

        private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SelectionButtonGroup)d;
            control.SyncPageWithSelection(e.NewValue);
            
            // Si el valor cambia a algo no nulo, notificar que se ha "marcado" una opción
            if (e.NewValue != null)
            {
                control.RaiseEvent(new RoutedEventArgs(CheckedEvent, control));
            }
        }

        private void SyncPageWithSelection(object selectedValue)
        {
            if (selectedValue == null || ItemsPerPage <= 0) return;

            // Buscamos en FilteredItems si existe, sino en ItemsSource
            var collection = (FilteredItems as System.Collections.Generic.IEnumerable<object>) ?? (ItemsSource?.Cast<object>().ToList());
            if (collection == null) return;

            int index = -1;
            int i = 0;
            foreach (var item in collection)
            {
                var val = GetPropertyValue(item, "Value");
                if (val != null && val.Equals(selectedValue))
                {
                    index = i;
                    break;
                }
                i++;
            }

            if (index != -1)
            {
                int targetPage = (index / ItemsPerPage) + 1;
                if (CurrentPage != targetPage)
                {
                    CurrentPage = targetPage;
                }
            }
        }

        private void OnSourceCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ApplyFilter(InlineSearchText);
            // Si hay algo seleccionado, sincronizamos página al cargar la colección
            if (SelectedValue != null) SyncPageWithSelection(SelectedValue);
        }

        private static void OnPaginationConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SelectionButtonGroup)d;
            control.CurrentPage = 1;
            control.CalculateTotalPages();
            control.UpdatePagedItems();
        }



        // Método invocado desde el XAML cuando se hace click en un RadioButton
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb)
            {
                SelectedValue = rb.Tag;
                // RaiseEvent ya se llama en OnSelectedValueChanged
            }
        }

        #endregion

        #region Search Logic

        private bool _isSearchDialogOpen;
        public bool IsSearchDialogOpen
        {
            get => _isSearchDialogOpen;
            set
            {
                if (_isSearchDialogOpen != value)
                {
                    _isSearchDialogOpen = value;
                    OnPropertyChanged(nameof(IsSearchDialogOpen));
                    if (value)
                    {
                         // Al abrir, resetear filtro
                         FilteredItems = new ObservableCollection<object>(ItemsSource?.Cast<object>() ?? new List<object>());
                         
                         // Foco al searchbox
                         _ = global::System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                             await global::System.Threading.Tasks.Task.Delay(100);
                             if (SearchBox != null) SearchBox.Focus();
                         });
                    }
                }
            }
        }

        private void UpdateSearchEnabled()
        {
            // Solo actualizamos automáticamente si no ha sido forzado por el usuario (o simplemente mantenemos la lógica)
            // Para mantener compatibilidad con el comportamiento anterior:
            // IsSearchEnabled = ItemsPerPage > 0 && TotalPages > 1;
        }

        private ObservableCollection<object> _filteredItems;
        public ObservableCollection<object> FilteredItems
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(nameof(FilteredItems)); }
        }

        private string _inlineSearchText;
        public string InlineSearchText
        {
            get => _inlineSearchText;
            set
            {
                if (_inlineSearchText != value)
                {
                    _inlineSearchText = value;
                    OnPropertyChanged(nameof(InlineSearchText));
                    ApplyFilter(value);
                }
            }
        }

        private void ApplyFilter(string query)
        {
            if (ItemsSource == null) return;
            
            query = query?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredItems = new ObservableCollection<object>(ItemsSource.Cast<object>());
            }
            else
            {
                var result = ItemsSource.Cast<object>()
                    .Where(val => {
                        var label = GetPropertyValue(val, "Label")?.ToString()?.ToLower() ?? "";
                        var value = GetPropertyValue(val, "Value")?.ToString()?.ToLower() ?? "";
                        return label.Contains(query) || value.Contains(query);
                    }).ToList();
                FilteredItems = new ObservableCollection<object>(result);
            }
            
            // Siempre actualizamos la paginación y los items mostrados
            CalculateTotalPagesFromFiltered();
            if (IsSearchInlineEnabled) CurrentPage = 1;
            UpdatePagedItemsFromFiltered();
        }

        private void CalculateTotalPagesFromFiltered()
        {
            int count = FilteredItems?.Count ?? 0;
            if (ItemsPerPage <= 0 || count == 0)
            {
                TotalPages = 1;
            }
            else
            {
                TotalPages = (int)Math.Ceiling((double)count / ItemsPerPage);
            }
        }

        private void UpdatePagedItemsFromFiltered()
        {
            PagedItems.Clear();
            if (FilteredItems == null) return;

            if (ItemsPerPage <= 0)
            {
                foreach (var item in FilteredItems) PagedItems.Add(item);
            }
            else
            {
                var paged = FilteredItems.Skip((CurrentPage - 1) * ItemsPerPage).Take(ItemsPerPage);
                foreach (var item in paged) PagedItems.Add(item);
            }
        }

        private ICommand _toggleSearchCommand;
        public ICommand ToggleSearchCommand => _toggleSearchCommand ??= new RelayCommand(() =>
        {
            IsSearchDialogOpen = !IsSearchDialogOpen;
        });

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is global::System.Windows.Controls.TextBox tb)
            {
                ApplyFilter(tb.Text);
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Down)
            {
                // Mover foco a la lista
                 if (SearchResultsList != null && SearchResultsList.Items.Count > 0)
                {
                    SearchResultsList.SelectedIndex = 0;
                    var item = SearchResultsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                    item?.Focus();
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                IsSearchDialogOpen = false;
                e.Handled = true;
            }
             else if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (FilteredItems != null && FilteredItems.Count == 1)
                {
                    SelectAndClose(FilteredItems[0]);
                    e.Handled = true;
                }
            }
        }

        private void SearchResultsList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (SearchResultsList.SelectedItem != null)
                {
                    SelectAndClose(SearchResultsList.SelectedItem);
                    e.Handled = true;
                }
            }
             else if (e.Key == System.Windows.Input.Key.Escape)
            {
                 // Volver al searchbox
                 SearchBox.Focus();
                 e.Handled = true;
            }
        }

        private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             // Opcional: Auto-seleccionar al cambiar en lista? No, mejor con Enter o Click.
             // Si el usuario usa mouse, click cierra.
             if (e.AddedItems.Count > 0 && Mouse.LeftButton == MouseButtonState.Pressed) 
             {
                 // Si fue click (aproximado)
                 SelectAndClose(e.AddedItems[0]);
             }
        }

        private void SelectAndClose(object item)
        {
            if (item == null) return;
            var val = GetPropertyValue(item, "Value");
            SelectedValue = val;
            _isClosingSearch = true; // Prevenir reapertura por foco recuperado
            IsSearchDialogOpen = false;
            RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
            
            // Auto Paginar
            if (ItemsSource != null)
            {
                var list = ItemsSource.Cast<object>().ToList();
                var index = list.IndexOf(item);
                if (index >= 0 && ItemsPerPage > 0)
                {
                    CurrentPage = (index / ItemsPerPage) + 1;
                }
            }
        }

        #endregion

        #region Overrides

        protected override void OnGotFocus(System.Windows.RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            
            if (_isClosingSearch)
            {
                _isClosingSearch = false; // Consumir flag
                return;
            }

            if (AutoOpenSearchOnFocus && IsSearchEnabled && !IsSearchDialogOpen)
            {
                IsSearchDialogOpen = true;
            }
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            // Tecla F3 para Búsqueda
            if (e.Key == System.Windows.Input.Key.F3 && IsSearchEnabled)
            {
                IsSearchDialogOpen = true;
                e.Handled = true;
                return;
            }

            // Manejo de teclas numéricas (1-9) para selección rápida
            int index = -1;

            if (e.Key >= System.Windows.Input.Key.D1 && e.Key <= System.Windows.Input.Key.D9)
                index = e.Key - System.Windows.Input.Key.D1;
            else if (e.Key >= System.Windows.Input.Key.NumPad1 && e.Key <= System.Windows.Input.Key.NumPad9)
                index = e.Key - System.Windows.Input.Key.NumPad1;

            if (index >= 0 && index < PagedItems.Count)
            {
                var item = PagedItems[index];
                // Buscamos el valor del item (Value) usando reflexión o path
                // Pero SelectedValue espera el objeto o el ID?
                // El ItemsControl en el XAML bindea Tag="{Binding Value}". Asumimos que PagedItems son los objetos.
                // Necesitamos extraer el 'Value' del objeto item.
                // Como es generico, tratemos de simular el click o setear SelectedValue.
                
                // Opción 1: Setear SelectedValue directamente si podemos obtener el valor.
                // El Binding de SelectedValue en el RadioButton compara 'Value'.
                
                // Vamos a intentar obtener la propiedad 'Value' del item mediante reflection simple
                var val = GetPropertyValue(item, "Value"); // Asumimos propiedad Value por convención de este proyecto
                if (val != null)
                {
                    SelectedValue = val;
                    // Forzar evento Checked para que la vista padre reaccione (Auto-Focus)
                    RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
                    e.Handled = true;
                }
            }
        }

        private object GetPropertyValue(object src, string propName)
        {
            if (src == null) return null;

            // Soporte para JsonElement
            if (src is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.Object && je.TryGetProperty(propName, out var element))
                {
                    return element.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => element.GetString(),
                        System.Text.Json.JsonValueKind.Number => element.GetDouble(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        _ => element.ToString()
                    };
                }
                return null;
            }

            var prop = src.GetType().GetProperty(propName);
            return prop?.GetValue(src, null);
        }

        #endregion

        #region Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateButtonStyle();
        }

        // ThemeColor
        public static readonly DependencyProperty ThemeColorProperty =
            DependencyProperty.Register("ThemeColor", typeof(System.Windows.Media.Brush), typeof(SelectionButtonGroup),
                new PropertyMetadata(null, OnThemeColorChanged));

        public System.Windows.Media.Brush ThemeColor
        {
            get { return (System.Windows.Media.Brush)GetValue(ThemeColorProperty); }
            set { SetValue(ThemeColorProperty, value); }
        }

        private static void OnThemeColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SelectionButtonGroup)d).UpdateButtonStyle();
        }

        private void UpdateButtonStyle()
        {
            // Determinar el color base
            System.Windows.Media.Color baseColor;

            if (ThemeColor is System.Windows.Media.SolidColorBrush solidBrush)
            {
                baseColor = solidBrush.Color;
            }
            else
            {
                // Default Blue (#3B82F6) si no se especifica ThemeColor
                baseColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3B82F6");
            }

            // Generar estilo dinámico
            CurrentButtonStyle = CreateDynamicStyle(baseColor);
        }

        private Style CreateDynamicStyle(System.Windows.Media.Color baseColor)
        {
            // Colores derivados
            var backgroundNormal = new System.Windows.Media.SolidColorBrush(ChangeColorBrightness(baseColor, 0.8f)); // Muy claro
            var borderNormal = new System.Windows.Media.SolidColorBrush(ChangeColorBrightness(baseColor, 0.4f)); // Claro
            var backgroundChecked = new System.Windows.Media.SolidColorBrush(baseColor); // Color original
            var borderChecked = new System.Windows.Media.SolidColorBrush(ChangeColorBrightness(baseColor, -0.2f)); // Oscuro
            
            // Estilo base
            var style = new Style(typeof(System.Windows.Controls.RadioButton));
            
            // Intentar basarse en MaterialDesignFlatButton si está disponible, sino UserControl default
            try 
            {
                if (System.Windows.Application.Current != null && System.Windows.Application.Current.TryFindResource("MaterialDesignFlatButton") is Style baseStyle)
                {
                    style.BasedOn = baseStyle;
                }
            }
            catch { /* Ignorar si no encuentra el recurso base */ }

            // Setters Base
            style.Setters.Add(new Setter(HeightProperty, 70.0));
            style.Setters.Add(new Setter(MarginProperty, new Thickness(4)));
            style.Setters.Add(new Setter(FontSizeProperty, 14.0));
            style.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
            style.Setters.Add(new Setter(CursorProperty, System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(2)));
            style.Setters.Add(new Setter(BackgroundProperty, backgroundNormal));
            style.Setters.Add(new Setter(BorderBrushProperty, borderNormal));
            style.Setters.Add(new Setter(ForegroundProperty, new System.Windows.Media.SolidColorBrush(ChangeColorBrightness(baseColor, -0.5f)))); // Texto oscuro
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, System.Windows.HorizontalAlignment.Center));
            style.Setters.Add(new Setter(VerticalContentAlignmentProperty, System.Windows.VerticalAlignment.Center));

            // Triggers
            
            // Trigger: IsChecked = True
            var checkedTrigger = new Trigger { Property = System.Windows.Controls.RadioButton.IsCheckedProperty, Value = true };
            checkedTrigger.Setters.Add(new Setter(BackgroundProperty, backgroundChecked));
            checkedTrigger.Setters.Add(new Setter(BorderBrushProperty, borderChecked));
            checkedTrigger.Setters.Add(new Setter(ForegroundProperty, System.Windows.Media.Brushes.White));
            
            // Sombra (Effect)
            var dropShadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 4,
                Opacity = 0.6,
                Color = baseColor
            };
            checkedTrigger.Setters.Add(new Setter(EffectProperty, dropShadow));
            
            style.Triggers.Add(checkedTrigger);

            // Trigger: IsMouseOver = True
            var mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(BorderBrushProperty, backgroundChecked));
            
            style.Triggers.Add(mouseOverTrigger);

            return style;
        }

        // Helper para modificar brillo de color
        // factor > 0: más claro (0..1), factor < 0: más oscuro (-1..0)
        private System.Windows.Media.Color ChangeColorBrightness(System.Windows.Media.Color color, float factor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (factor < 0)
            {
                factor = 1 + factor;
                red *= factor;
                green *= factor;
                blue *= factor;
            }
            else
            {
                red = (255 - red) * factor + red;
                green = (255 - green) * factor + green;
                blue = (255 - blue) * factor + blue;
            }

            return System.Windows.Media.Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }

        private void CalculateTotalPages()
        {
            if (ItemsSource == null)
            {
                TotalPages = 1;
                return;
            }

            int count = 0;
            foreach (var item in ItemsSource) count++;

            if (ItemsPerPage <= 0 || count == 0)
            {
                TotalPages = 1;
            }
            else
            {
                TotalPages = (int)Math.Ceiling((double)count / ItemsPerPage);
            }
        }

        private void UpdatePagedItems()
        {
            PagedItems.Clear();
            if (ItemsSource == null) return;

            var sourceList = new System.Collections.Generic.List<object>();
            foreach (var item in ItemsSource) sourceList.Add(item);

            if (ItemsPerPage <= 0)
            {
                // Mostrar todo
                foreach (var item in sourceList) PagedItems.Add(item);
            }
            else
            {
                // Paginar
                var paged = sourceList.Skip((CurrentPage - 1) * ItemsPerPage).Take(ItemsPerPage);
                foreach (var item in paged) PagedItems.Add(item);
            }
        }

        public void FocusSearch()
        {
            this.UpdateLayout();
            if (IsComboBoxVisible && InternalComboBox != null)
            {
                InternalComboBox.Focus();
                // Opcionalmente: Keyboard.Focus(InternalComboBox);
            }
            else if (IsSearchInlineEnabled && InlineSearchBox != null)
            {
                InlineSearchBox.Focus();
            }
            else
            {
                this.Focus();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
