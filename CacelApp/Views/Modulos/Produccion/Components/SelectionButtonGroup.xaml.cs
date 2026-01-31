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

namespace CacelApp.Views.Modulos.Produccion.Components
{
    public partial class SelectionButtonGroup : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public SelectionButtonGroup()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this; 
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
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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
                    UpdatePagedItems();
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
            control.CalculateTotalPages();
            control.UpdatePagedItems();
            
            // Hack para suscribirse a cambios en la colección
            if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= control.OnSourceCollectionChanged;
            
            if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                newColl.CollectionChanged += control.OnSourceCollectionChanged;
        }

        private void OnSourceCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            CalculateTotalPages();
            UpdatePagedItems();
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
                RaiseEvent(new RoutedEventArgs(CheckedEvent, this));
            }
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
