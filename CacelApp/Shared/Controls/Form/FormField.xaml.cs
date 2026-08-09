using System.Text.RegularExpressions;
using UserControl = System.Windows.Controls.UserControl;

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
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty RequiredProperty =
        DependencyProperty.Register(nameof(Required), typeof(bool), typeof(FormField),
            new PropertyMetadata(false, OnRequiredChanged));

    public static readonly DependencyProperty DisplayLabelProperty =
        DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(FieldVariant), typeof(FormField),
            new PropertyMetadata(FieldVariant.Text, OnVariantChanged));

    public static readonly DependencyProperty HelperTextProperty =
        DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormField),
            new PropertyMetadata(string.Empty, OnHelperTextChanged));

    public static readonly DependencyProperty DisplayHelperTextProperty =
        DependencyProperty.Register(nameof(DisplayHelperText), typeof(string), typeof(FormField), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HasValidationErrorProperty =
        DependencyProperty.Register(nameof(HasValidationError), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormField), new PropertyMetadata(true));

    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(FormField), new PropertyMetadata(0));

    public static readonly DependencyProperty MinLengthProperty =
        DependencyProperty.Register(nameof(MinLength), typeof(int), typeof(FormField),
            new PropertyMetadata(0, OnMinLengthChanged));

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

    public static readonly DependencyProperty ClearOnFocusProperty =
        DependencyProperty.Register(nameof(ClearOnFocus), typeof(bool), typeof(FormField), new PropertyMetadata(false));

    public static readonly DependencyProperty CharacterCasingProperty =
        DependencyProperty.Register(nameof(CharacterCasing), typeof(System.Windows.Controls.CharacterCasing), typeof(FormField),
            new PropertyMetadata(System.Windows.Controls.CharacterCasing.Normal));

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

    public string DisplayHelperText
    {
        get => (string)GetValue(DisplayHelperTextProperty);
        private set => SetValue(DisplayHelperTextProperty, value);
    }

    public bool HasValidationError
    {
        get => (bool)GetValue(HasValidationErrorProperty);
        private set => SetValue(HasValidationErrorProperty, value);
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

    public bool ClearOnFocus
    {
        get => (bool)GetValue(ClearOnFocusProperty);
        set => SetValue(ClearOnFocusProperty, value);
    }

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public int MinLength
    {
        get => (int)GetValue(MinLengthProperty);
        set => SetValue(MinLengthProperty, value);
    }

    public System.Windows.Controls.CharacterCasing CharacterCasing
    {
        get => (System.Windows.Controls.CharacterCasing)GetValue(CharacterCasingProperty);
        set => SetValue(CharacterCasingProperty, value);
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

    private string _previousValue = string.Empty;

    public FormField()
    {
        InitializeComponent();
        TextBoxControl.PreviewTextInput += OnPreviewTextInput;
        TextBoxControl.GotFocus += OnTextBoxGotFocus;
        TextBoxControl.LostFocus += OnTextBoxLostFocus;
        UpdateDisplayLabel();
        ValidateValue();
    }

    private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (ClearOnFocus)
        {
            _previousValue = Value ?? string.Empty;
            TextBoxControl.SelectAll();
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (ClearOnFocus && string.IsNullOrWhiteSpace(Value))
        {
            Value = _previousValue;
        }

        ValidateValue();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.ValidateValue();
        }
    }

    private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.UpdateDisplayLabel();
            field.ValidateValue();
        }
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.UpdateDisplayLabel();
        }
    }

    private static void OnHelperTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.ValidateValue();
        }
    }

    private static void OnMinLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field)
        {
            field.ValidateValue();
        }
    }

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
    }

    private void ValidateValue()
    {
        var text = Value ?? string.Empty;

        if (MinLength > 0 && !string.IsNullOrEmpty(text) && text.Length < MinLength)
        {
            HasValidationError = true;
            DisplayHelperText = $"Mínimo {MinLength} dígitos ({text.Length}/{MinLength})";
            return;
        }

        if (Required && string.IsNullOrWhiteSpace(text))
        {
            HasValidationError = true;
            DisplayHelperText = "Este campo es requerido";
            return;
        }

        HasValidationError = false;
        DisplayHelperText = HelperText;
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormField field && e.NewValue is FieldVariant variant)
        {
            switch (variant)
            {
                case FieldVariant.Password:
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
        bool isValid = true;

        switch (Variant)
        {
            case FieldVariant.Number:
                isValid = IsNumericInput(e.Text);
                break;
            case FieldVariant.Decimal:
                isValid = IsDecimalInput(e.Text);
                break;
        }

        if (!isValid)
        {
            e.Handled = true;
        }
    }

    private bool IsNumericInput(string text)
    {
        return Regex.IsMatch(text, @"^[0-9]+$");
    }

    private bool IsDecimalInput(string text)
    {
        if (!Regex.IsMatch(text, @"^[0-9.]+$")) return false;

        var currentText = TextBoxControl.Text;
        var caretIndex = TextBoxControl.CaretIndex;
        var newText = currentText.Insert(caretIndex, text);

        return newText.Count(c => c == '.') <= 1;
    }
}
