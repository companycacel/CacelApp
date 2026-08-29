using System.Collections;
using System.Windows.Data;
using System.Windows.Markup;
using UserControl = System.Windows.Controls.UserControl;

namespace CacelApp.Shared.Controls.Form
{
    [ContentProperty(nameof(InlineOptions))]
    public partial class FormComboBox : UserControl, IAddChild
    {
        public List<ComboBoxOption> InlineOptions { get; } = new();

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormComboBox),
                new PropertyMetadata(string.Empty, OnLabelChanged));

        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register(nameof(Options), typeof(IEnumerable), typeof(FormComboBox),
                new PropertyMetadata(null, OnOptionsChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(FormComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty ExtDataProperty =
            DependencyProperty.Register(nameof(ExtData), typeof(object), typeof(FormComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(FormComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty RequiredProperty =
            DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormComboBox),
                new PropertyMetadata(false, OnRequiredChanged));

        public static readonly DependencyProperty DisplayLabelProperty =
            DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormComboBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HelperTextProperty =
            DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormComboBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CustomStyleProperty =
            DependencyProperty.Register(nameof(CustomStyle), typeof(Style), typeof(FormComboBox),
                new PropertyMetadata(null, OnCustomStyleChanged));

        public static readonly DependencyProperty IsFilterEnabledProperty =
            DependencyProperty.Register(nameof(IsFilterEnabled), typeof(bool), typeof(FormComboBox),
                new PropertyMetadata(false, OnIsFilterEnabledChanged));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public IEnumerable Options
        {
            get => (IEnumerable)GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public object ExtData
        {
            get => GetValue(ExtDataProperty);
            set => SetValue(ExtDataProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public bool Required
        {
            get => (bool)GetValue(RequiredProperty);
            set => SetValue(RequiredProperty, value);
        }

        public string DisplayLabel
        {
            get => (string)GetValue(DisplayLabelProperty);
            private set => SetValue(DisplayLabelProperty, value);
        }

        public string HelperText
        {
            get => (string)GetValue(HelperTextProperty);
            set => SetValue(HelperTextProperty, value);
        }

        // Usar la propiedad IsEnabled heredada (no volver a declararla)

        public Style CustomStyle
        {
            get => (Style)GetValue(CustomStyleProperty);
            set => SetValue(CustomStyleProperty, value);
        }

        public bool IsFilterEnabled
        {
            get => (bool)GetValue(IsFilterEnabledProperty);
            set => SetValue(IsFilterEnabledProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => ComboBoxControl.IsDropDownOpen;
            set => ComboBoxControl.IsDropDownOpen = value;
        }

        static FormComboBox()
        {
            FontSizeProperty.OverrideMetadata(typeof(FormComboBox), new FrameworkPropertyMetadata(13.0));
        }

        public FormComboBox()
        {
            InitializeComponent();
            UpdateDisplayLabel();

            // Suscribirse al evento Loaded para sincronizar el valor seleccionado
            Loaded += FormComboBox_Loaded;

            // ✅ Suscribirse al evento Unloaded para limpiar el filtro
            Unloaded += FormComboBox_Unloaded;

            // Suscribirse al evento SelectionChanged para actualizar ExtData y SelectedItem
            ComboBoxControl.SelectionChanged += ComboBoxControl_SelectionChanged;

            // Manejar filtrado
            ComboBoxControl.KeyUp += ComboBoxControl_KeyUp;

            // Reenviar foco al control interno cuando el UserControl recibe el foco
            GotFocus += (s, e) =>
            {
                if (e.OriginalSource == this)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ComboBoxControl.IsEditable)
                        {
                            var textBox = (System.Windows.Controls.TextBox)ComboBoxControl.Template.FindName("PART_EditableTextBox", ComboBoxControl);
                            if (textBox != null)
                            {
                                Keyboard.Focus(textBox);
                                textBox.CaretIndex = textBox.Text.Length;
                                return;
                            }
                        }
                        Keyboard.Focus(ComboBoxControl);
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            };
        }

        private void FormComboBox_Unloaded(object sender, RoutedEventArgs e)
        {
            // ✅ Limpiar el filtro del CollectionView cuando se descarga el control
            if (IsFilterEnabled && ComboBoxControl.ItemsSource != null)
            {
                var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                if (view != null)
                {
                    view.Filter = null;
                }
            }
        }        private static bool ValuesAreEqual(object? val1, object? val2)
        {
            if (Equals(val1, val2)) return true;
            if (val1 == null || val2 == null) return false;
            return string.Equals(val1.ToString(), val2.ToString(), StringComparison.Ordinal);
        }

        private void ComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
            {
                SetCurrentValue(ExtDataProperty, option.Ext);
                SetCurrentValue(SelectedItemProperty, option);
                if (!ValuesAreEqual(Value, option.Value))
                {
                    SetCurrentValue(ValueProperty, option.Value);
                }
            }
            else if (ComboBoxControl.SelectedItem != null)
            {
                SetCurrentValue(ExtDataProperty, null);
                SetCurrentValue(SelectedItemProperty, ComboBoxControl.SelectedItem);
            }
            else
            {
                SetCurrentValue(ExtDataProperty, null);
                SetCurrentValue(SelectedItemProperty, null);
            }
        }

        private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control)
            {
                var items = e.NewValue as IEnumerable;

                if (items != null)
                {
                    // Normalizar option.Value en-place para que SelectedValuePath funcione
                    // con propiedades int? en el ViewModel (cubre cargas asíncronas post-Loaded)
                    foreach (var item in items)
                    {
                        if (item is Core.Shared.Entities.SelectOption opt && opt.Value != null)
                        {
                            var v = opt.Value;
                            if (v is System.Text.Json.JsonElement je)
                            {
                                if (je.ValueKind == System.Text.Json.JsonValueKind.Number && je.TryGetInt32(out int parsed))
                                    opt.Value = parsed;
                                else if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                                    opt.Value = je.GetString();
                            }
                            else if (v is long l) opt.Value = (int)l;
                            else if (v is double db) opt.Value = (int)db;
                            else if (v is decimal dc) opt.Value = (int)dc;
                            else if (v is float f) opt.Value = (int)f;
                        }
                    }

                    control.ComboBoxControl.ItemsSource = items;
                    control.ComboBoxControl.DisplayMemberPath = "Label";
                    control.ComboBoxControl.SelectedValuePath = "Value";

                    if (control.IsFilterEnabled)
                    {
                        control.SetupFiltering();
                    }

                    // Re-sincronizar el valor seleccionado si ya hay un Value sin romper bindings
                    if (control.Value != null)
                    {
                        control.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            control.ComboBoxControl.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, control.Value);
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
                else if (control.InlineOptions.Count > 0)
                {
                    control.ComboBoxControl.ItemsSource = control.InlineOptions;
                    control.ComboBoxControl.DisplayMemberPath = "Label";
                    control.ComboBoxControl.SelectedValuePath = "Value";
                }
                else
                {
                    control.ComboBoxControl.ItemsSource = null;
                }
            }
        }

        private void FormComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Forzar la sincronización del valor seleccionado después de que el control esté cargado
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ComboBoxControl.ItemsSource != null)
                {
                    foreach (var item in ComboBoxControl.ItemsSource)
                    {
                        if (item is Core.Shared.Entities.SelectOption option && option.Value != null)
                        {
                            var originalValue = option.Value;
                            var originalType = originalValue.GetType();

                            if (originalType.Name == "JsonElement")
                            {
                                var jsonElement = (System.Text.Json.JsonElement)originalValue;

                                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                                {
                                    if (jsonElement.TryGetInt32(out int intValue))
                                    {
                                        option.Value = intValue;
                                    }
                                    else if (jsonElement.TryGetInt64(out long longValue))
                                    {
                                        option.Value = (int)longValue;
                                    }
                                }
                                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    option.Value = jsonElement.GetString();
                                }
                            }
                            else if (originalValue is long l)
                            {
                                option.Value = (int)l;
                            }
                            else if (originalValue is decimal d)
                            {
                                option.Value = (int)d;
                            }
                            else if (originalValue is double db)
                            {
                                option.Value = (int)db;
                            }
                            else if (originalValue is float f)
                            {
                                option.Value = (int)f;
                            }
                        }
                    }
                }

                // Sincronizar el valor seleccionado sin destruir la expresión de Binding
                if (Value != null && ComboBoxControl.ItemsSource != null)
                {
                    var normalizedValue = Value;
                    if (Value is long l)
                        normalizedValue = (int)l;
                    else if (Value is decimal d)
                        normalizedValue = (int)d;
                    else if (Value is double db)
                        normalizedValue = (int)db;
                    else if (Value is float f)
                        normalizedValue = (int)f;

                    ComboBoxControl.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, normalizedValue);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        public void AddChild(object value)
        {
            if (value is ComboBoxOption option)
                InlineOptions.Add(option);
        }

        public void AddText(string text)
        {
            // No se usa, pero debe existir
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (InlineOptions.Count > 0 && (Options == null))
            {
                ComboBoxControl.ItemsSource = InlineOptions;
                ComboBoxControl.DisplayMemberPath = "Label";
                ComboBoxControl.SelectedValuePath = "Value";
            }
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox combo) combo.UpdateDisplayLabel();
        }

        private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox combo) combo.UpdateDisplayLabel();
        }

        private void UpdateDisplayLabel() => DisplayLabel = Required ? $"{Label} *" : Label;

        private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control && e.NewValue is Style style)
                control.ComboBoxControl.Style = style;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control && e.NewValue != null)
            {
                // Actualizar ComboBoxControl.SelectedValue usando SetCurrentValue
                control.ComboBoxControl.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, e.NewValue);

                // Cuando Value cambia, actualizar ExtData y SelectedItem si encontramos el item correspondiente
                if (control.ComboBoxControl.ItemsSource != null)
                {
                    foreach (var item in control.ComboBoxControl.ItemsSource)
                    {
                        if (item is Core.Shared.Entities.SelectOption option && option.Value != null)
                        {
                            if (ValuesAreEqual(option.Value, e.NewValue))
                            {
                                control.SetCurrentValue(ExtDataProperty, option.Ext);
                                control.SetCurrentValue(SelectedItemProperty, option);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void OnIsFilterEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control)
            {
                bool isEnabled = (bool)e.NewValue;
                control.ComboBoxControl.IsEditable = isEnabled;
                control.ComboBoxControl.IsTextSearchEnabled = !isEnabled;

                if (isEnabled)
                {
                    control.SetupFiltering();
                }
            }
        }

        private void SetupFiltering()
        {
            if (ComboBoxControl.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);

            // ✅ Solo asignar el filtro si no está ya asignado para evitar redundancia
            if (view.Filter != FilterPredicate)
            {
                view.Filter = FilterPredicate;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (string.IsNullOrEmpty(ComboBoxControl.Text)) return true;

            if (obj is Core.Shared.Entities.SelectOption option)
            {
                return option.Label.Contains(ComboBoxControl.Text, StringComparison.OrdinalIgnoreCase);
            }

            if (obj is ComboBoxOption inlineOption)
            {
                return inlineOption.Label.Contains(ComboBoxControl.Text, StringComparison.OrdinalIgnoreCase);
            }

            return obj.ToString()?.Contains(ComboBoxControl.Text, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private void ComboBoxControl_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsFilterEnabled) return;

            // Ignorar teclas de navegación para no refrescar el filtro innecesariamente
            if (e.Key == System.Windows.Input.Key.Down ||
                e.Key == System.Windows.Input.Key.Up ||
                e.Key == System.Windows.Input.Key.Enter ||
                e.Key == System.Windows.Input.Key.Tab ||
                e.Key == System.Windows.Input.Key.Left ||
                e.Key == System.Windows.Input.Key.Right)
            {
                return;
            }

            // Obtener el TextBox interno para gestionar el cursor y la selección
            var textBox = (System.Windows.Controls.TextBox)ComboBoxControl.Template.FindName("PART_EditableTextBox", ComboBoxControl);

            // Guardar posición del cursor y selección
            int caretIndex = textBox?.CaretIndex ?? 0;
            int selectionLength = textBox?.SelectionLength ?? 0;

            // Si el texto ha cambiado y ya no coincide con la selección actual, limpiar la selección
            // Esto evita que al refrescar la vista, el ComboBox restaure el texto del item seleccionado
            if (ComboBoxControl.SelectedItem != null)
            {
                string currentLabel = string.Empty;
                if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
                    currentLabel = option.Label;
                else if (ComboBoxControl.SelectedItem is ComboBoxOption inlineOption)
                    currentLabel = inlineOption.Label;
                else
                    currentLabel = ComboBoxControl.SelectedItem.ToString() ?? string.Empty;

                if (!string.Equals(ComboBoxControl.Text, currentLabel, StringComparison.OrdinalIgnoreCase))
                {
                    ComboBoxControl.SelectedItem = null;
                }
            }

            // Actualizar filtro al escribir
            var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
            if (view != null)
            {
                view.Refresh();

                // Abrir dropdown si hay texto y no está abierto
                if (!string.IsNullOrEmpty(ComboBoxControl.Text) && !ComboBoxControl.IsDropDownOpen)
                {
                    ComboBoxControl.IsDropDownOpen = true;
                }

                // Restaurar posición del cursor y limpiar selección para evitar sobrescritura
                if (textBox != null)
                {
                    textBox.CaretIndex = caretIndex;
                    textBox.SelectionLength = 0;
                }
            }
        }

        private void ComboBoxControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsFilterEnabled) return;

            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (!ComboBoxControl.IsDropDownOpen)
                {
                    ComboBoxControl.IsDropDownOpen = true;
                    e.Handled = true;
                    return;
                }

                // Navegación manual hacia abajo
                var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                if (view != null)
                {
                    var items = view.Cast<object>().ToList();
                    if (items.Count > 0)
                    {
                        if (ComboBoxControl.SelectedItem == null)
                        {
                            // Si no hay nada seleccionado, seleccionar el primero
                            ComboBoxControl.SelectedItem = items[0];
                        }
                        else
                        {
                            // Buscar el índice actual y mover al siguiente
                            int index = items.IndexOf(ComboBoxControl.SelectedItem);
                            if (index < items.Count - 1)
                            {
                                ComboBoxControl.SelectedItem = items[index + 1];
                            }
                        }

                        // Asegurar que el texto se actualice y el cursor vaya al final
                        UpdateTextAndCaret();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                if (ComboBoxControl.IsDropDownOpen)
                {
                    // Navegación manual hacia arriba
                    var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                    if (view != null)
                    {
                        var items = view.Cast<object>().ToList();
                        if (items.Count > 0 && ComboBoxControl.SelectedItem != null)
                        {
                            int index = items.IndexOf(ComboBoxControl.SelectedItem);
                            if (index > 0)
                            {
                                ComboBoxControl.SelectedItem = items[index - 1];
                                UpdateTextAndCaret();
                                e.Handled = true;
                            }
                        }
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
            {
                if (ComboBoxControl.IsDropDownOpen)
                {
                    // Si hay un item seleccionado (navegación con flechas), usar ese
                    if (ComboBoxControl.SelectedItem != null)
                    {
                        // Ya está seleccionado, solo cerrar si es Enter
                        if (e.Key == System.Windows.Input.Key.Enter)
                        {
                            ComboBoxControl.IsDropDownOpen = false;
                            e.Handled = true;
                        }
                        return;
                    }

                    // Si no hay selección, tomar el primero del filtro
                    var view = CollectionViewSource.GetDefaultView(ComboBoxControl.ItemsSource);
                    if (view != null)
                    {
                        var firstItem = view.Cast<object>().FirstOrDefault();
                        if (firstItem != null)
                        {
                            ComboBoxControl.SelectedItem = firstItem;
                            UpdateTextAndCaret();
                        }
                    }

                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        ComboBoxControl.IsDropDownOpen = false;
                        e.Handled = true;
                    }
                }
            }
        }

        private void UpdateTextAndCaret()
        {
            // Actualizar texto visualmente (aunque el binding lo haga, forzamos para el caret)
            if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
                ComboBoxControl.Text = option.Label;
            else if (ComboBoxControl.SelectedItem is ComboBoxOption inlineOption)
                ComboBoxControl.Text = inlineOption.Label;

            // Mover cursor al final
            var textBox = (System.Windows.Controls.TextBox)ComboBoxControl.Template.FindName("PART_EditableTextBox", ComboBoxControl);
            if (textBox != null)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    public class ComboBoxOption : DependencyObject
    {
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public object? Ext { get; set; }
        public bool Disabled { get; set; }
    }
}
