using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace CacelApp.Shared.Controls.Form;

public partial class FormComboBox : UserControl
{
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
        DependencyProperty.Register(nameof(DisplayLabel), typeof(string), typeof(FormComboBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HelperTextProperty =
        DependencyProperty.Register(nameof(HelperText), typeof(string), typeof(FormComboBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(FormComboBox), new PropertyMetadata(true));

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

    public new bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

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

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormComboBox combo)
        {
            combo.UpdateDisplayLabel();
        }
    }

    private static void OnRequiredChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormComboBox combo)
        {
            combo.UpdateDisplayLabel();
        }
    }

    private void UpdateDisplayLabel()
    {
        DisplayLabel = Required ? $"{Label} *" : Label;
    }

    private static void OnOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormComboBox control)
        {
            control.ComboBoxControl.ItemsSource = e.NewValue as IEnumerable;
            control.ComboBoxControl.DisplayMemberPath = "Label";
            control.ComboBoxControl.SelectedValuePath = "Value";
        }
    }

    private static void OnCustomStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormComboBox control && e.NewValue is Style style)
        {
            control.ComboBoxControl.Style = style;
        }
    }
}
