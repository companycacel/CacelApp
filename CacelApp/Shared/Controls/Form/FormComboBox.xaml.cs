using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

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

                // Si Options es v�lido y tiene al menos un elemento
                if (items != null && items.GetEnumerator().MoveNext())
                {
                    control.ComboBoxControl.ItemsSource = items;
                    control.ComboBoxControl.DisplayMemberPath = "Label";
                    control.ComboBoxControl.SelectedValuePath = "Value";
                }
                // Si no hay Options pero s� opciones inline
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
    }

    public class ComboBoxOption : DependencyObject
    {
        public string Label { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
    }
}
