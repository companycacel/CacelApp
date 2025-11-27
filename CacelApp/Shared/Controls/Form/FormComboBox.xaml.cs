using System.Collections;
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

        public FormComboBox()
        {
            InitializeComponent();
            UpdateDisplayLabel();

            // Suscribirse al evento Loaded para sincronizar el valor seleccionado
            Loaded += FormComboBox_Loaded;

            // Suscribirse al evento SelectionChanged para actualizar ExtData y SelectedItem
            ComboBoxControl.SelectionChanged += ComboBoxControl_SelectionChanged;
        }

        private void ComboBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Actualizar ExtData y SelectedItem cuando cambia la selección
            if (ComboBoxControl.SelectedItem is Core.Shared.Entities.SelectOption option)
            {
                ExtData = option.Ext;
                SelectedItem = option;
            }
            else
            {
                ExtData = null;
                SelectedItem = ComboBoxControl.SelectedItem;
            }
        }

        private void FormComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Forzar la sincronización del valor seleccionado después de que el control esté cargado
            // Esto asegura que el binding funcione correctamente cuando el valor se setea antes de ShowDialog()
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Normalizar los valores de las opciones para asegurar comparación correcta
                // Convertir JsonElement y otros tipos numéricos a sus tipos base
                if (ComboBoxControl.ItemsSource != null)
                {
                    foreach (var item in ComboBoxControl.ItemsSource)
                    {
                        if (item is Core.Shared.Entities.SelectOption option && option.Value != null)
                        {
                            var originalValue = option.Value;
                            var originalType = originalValue.GetType();

                            // CRÍTICO: Manejar JsonElement (viene de deserialización JSON de API)
                            if (originalType.Name == "JsonElement")
                            {
                                var jsonElement = (System.Text.Json.JsonElement)originalValue;

                                // Convertir según el tipo del JsonElement
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
                            // Convertir otros tipos numéricos a int
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

                // Sincronizar el valor seleccionado
                if (Value != null && ComboBoxControl.ItemsSource != null)
                {
                    // Normalizar el valor actual si es necesario
                    var normalizedValue = Value;
                    if (Value is long l)
                        normalizedValue = (int)l;
                    else if (Value is decimal d)
                        normalizedValue = (int)d;
                    else if (Value is double db)
                        normalizedValue = (int)db;
                    else if (Value is float f)
                        normalizedValue = (int)f;

                    ComboBoxControl.SelectedValue = normalizedValue;
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

        private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control)
            {
                var items = e.NewValue as IEnumerable;

                // Si Options es válido (aunque esté vacío, para soportar ObservableCollection)
                if (items != null)
                {
                    control.ComboBoxControl.ItemsSource = items;
                    control.ComboBoxControl.DisplayMemberPath = "Label";
                    control.ComboBoxControl.SelectedValuePath = "Value";
                }
                // Si no hay Options pero sí opciones inline
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

        private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control && e.NewValue is Style style)
                control.ComboBoxControl.Style = style;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FormComboBox control && e.NewValue != null)
            {
                // Cuando Value cambia, actualizar ExtData si encontramos el item correspondiente
                if (control.ComboBoxControl.ItemsSource != null)
                {
                    foreach (var item in control.ComboBoxControl.ItemsSource)
                    {
                        if (item is Core.Shared.Entities.SelectOption option && 
                            option.Value != null && 
                            option.Value.Equals(e.NewValue))
                        {
                            control.ExtData = option.Ext;
                            control.SelectedItem = option;
                            break;
                        }
                    }
                }
            }
        }
    }

    public class ComboBoxOption : DependencyObject
    {
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public object? Ext { get; set; }
    }
}
