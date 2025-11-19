using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CacelApp.Shared.Controls.Form;

public enum FieldVariant
{
    Text,
    Number,
    Decimal,
    Email,
    Password,
    TextArea
}

public partial class FormField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormField), 
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(FormField), 
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty RequiredProperty =
        DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormField), 
            new PropertyMetadata(false, OnRequiredChanged));

    public static readonly DependencyProperty DisplayLabelProperty =
        DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(FieldVariant), typeof(FormField), 
            new PropertyMetadata(FieldVariant.Text, OnVariantChanged));

    public static readonly DependencyProperty HelperTextProperty =
        DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormField), new PropertyMetadata(true));

    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(FormField), new PropertyMetadata(0));

    public static readonly DependencyProperty CustomStyleProperty =
        DependencyProperty.Register(nameof(CustomStyle), typeof(Style), typeof(FormField), 
            new PropertyMetadata(null, OnCustomStyleChanged));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(FormField), 
            new PropertyMetadata(TextWrapping.NoWrap));

    public static readonly DependencyProperty AcceptsReturnProperty =
        DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public static readonly DependencyProperty MinHeightProperty =
        DependencyProperty.Register(nameof(MinHeight), typeof(double), typeof(FormField), new PropertyMetadata(0.0));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
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

    public FieldVariant Variant
    {
        get => (FieldVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public string HelperText
    {
        get => (string)GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public Style CustomStyle
    {
        get => (Style)GetValue(CustomStyleProperty);
        set => SetValue(CustomStyleProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public bool AcceptsReturn
    {
        get => (bool)GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public new double MinHeight
    {
        get => (double)GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public FormField()
    {
        InitializeComponent();
        TextBoxControl.PreviewTextInput += OnPreviewTextInput;
        UpdateDisplayLabel();
    }

    private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.UpdateDisplayLabel();
        }
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.UpdateDisplayLabel();
        }
    }

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field && e.NewValue is FieldVariant variant)
        {
            switch (variant)
            {
                case FieldVariant.Password:
                    // TODO: Cambiar a PasswordBox si es necesario
                    break;
                case FieldVariant.TextArea:
                    field.TextWrapping = TextWrapping.Wrap;
                    field.AcceptsReturn = true;
                    field.MinHeight = 80;
                    break;
            }
        }
    }

    private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field && e.NewValue is Style style)
        {
            field.TextBoxControl.Style = style;
        }
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        switch (Variant)
        {
            case FieldVariant.Number:
                e.Handled = !IsNumericInput(e.Text);
                break;
            case FieldVariant.Decimal:
                e.Handled = !IsDecimalInput(e.Text);
                break;
            case FieldVariant.Email:
                // Validación básica, se puede mejorar
                break;
        }
    }

    private bool IsNumericInput(string text)
    {
        return Regex.IsMatch(text, @"^[0-9]+$");
    }

    private bool IsDecimalInput(string text)
    {
        var currentText = Value ?? string.Empty;
        var newText = currentText + text;
        return Regex.IsMatch(text, @"^[0-9.]+$") && newText.Count(c => c == '.') <= 1;
    }
}
