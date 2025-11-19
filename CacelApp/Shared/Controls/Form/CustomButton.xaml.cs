using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CacelApp.Shared.Controls.Form;

public enum ButtonVariant
{
    Primary,      // Botón principal azul
    Success,      // Verde - para acciones positivas (guardar, agregar)
    Warning,      // Naranja - para advertencias
    Danger,       // Rojo - para acciones destructivas
    Info,         // Cyan - para información
    Secondary,    // Outlined/secundario
    Add,          // Variante específica para agregar
    Save,         // Variante específica para guardar
    Search,       // Variante específica para buscar
    Close,        // Variante específica para cerrar
    Delete,       // Variante específica para eliminar
    Edit,         // Variante específica para editar
    Print,        // Variante específica para imprimir
    Custom        // Personalizado
}

public partial class CustomButton : UserControl
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(CustomButton), new PropertyMetadata("Button"));

    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(PackIconKind), typeof(CustomButton), new PropertyMetadata(PackIconKind.Check));

    public static readonly DependencyProperty ButtonStyleProperty =
        DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty BackgroundColorProperty =
        DependencyProperty.Register(nameof(BackgroundColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(null));

    public static readonly DependencyProperty TextColorProperty =
        DependencyProperty.Register(nameof(TextColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(CustomButton), new PropertyMetadata(Brushes.White));

    public static readonly DependencyProperty HeightProperty =
        DependencyProperty.Register(nameof(Height), typeof(double), typeof(CustomButton), new PropertyMetadata(36.0));

    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(CustomButton), new PropertyMetadata(new Thickness(16, 8, 16, 8)));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(CustomButton), new PropertyMetadata(5.0));

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(CustomButton), new PropertyMetadata(18.0));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(CustomButton), new PropertyMetadata(13.0));

    public static readonly DependencyProperty FontWeightProperty =
        DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(CustomButton), new PropertyMetadata(FontWeights.Medium));

    public static readonly DependencyProperty ToolTipTextProperty =
        DependencyProperty.Register(nameof(ToolTipText), typeof(string), typeof(CustomButton), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(ButtonVariant), typeof(CustomButton), 
            new PropertyMetadata(ButtonVariant.Primary, OnVariantChanged));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public Style ButtonStyle
    {
        get => (Style)GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    public Brush BackgroundColor
    {
        get => (Brush)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public Brush TextColor
    {
        get => (Brush)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Brush IconColor
    {
        get => (Brush)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public new double Height
    {
        get => (double)GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public new Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public new double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public string ToolTipText
    {
        get => (string)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    public ButtonVariant Variant
    {
        get => (ButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public CustomButton()
    {
        InitializeComponent();
        ApplyVariant(Variant);
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomButton button)
        {
            button.ApplyVariant((ButtonVariant)e.NewValue);
        }
    }

    private void ApplyVariant(ButtonVariant variant)
    {
        var primaryColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
        var successColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        var warningColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
        var dangerColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
        var infoColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ACC1"));
        var themeRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));

        switch (variant)
        {
            case ButtonVariant.Primary:
                SetVariantDefaults("Aceptar", PackIconKind.Check, primaryColor, true);
                break;

            case ButtonVariant.Success:
                SetVariantDefaults("Guardar", PackIconKind.ContentSave, successColor, true);
                break;

            case ButtonVariant.Warning:
                SetVariantDefaults("Advertencia", PackIconKind.Alert, warningColor, true);
                break;

            case ButtonVariant.Danger:
                SetVariantDefaults("Eliminar", PackIconKind.Delete, dangerColor, true);
                break;

            case ButtonVariant.Info:
                SetVariantDefaults("Información", PackIconKind.Information, infoColor, true);
                break;

            case ButtonVariant.Secondary:
                SetVariantDefaults("Cancelar", PackIconKind.Close, null, false);
                if (ButtonStyle == null)
                {
                    ButtonStyle = Application.Current.TryFindResource("MaterialDesignOutlinedButton") as Style;
                }
                break;

            case ButtonVariant.Add:
                SetVariantDefaults("Nuevo", PackIconKind.Plus, themeRed, true);
                break;

            case ButtonVariant.Save:
                SetVariantDefaults("Guardar", PackIconKind.ContentSave, successColor, true);
                break;

            case ButtonVariant.Search:
                SetVariantDefaults("Buscar", PackIconKind.Filter, primaryColor, true);
                break;

            case ButtonVariant.Close:
                SetVariantDefaults("Cerrar", PackIconKind.Close, warningColor, true);
                break;

            case ButtonVariant.Delete:
                SetVariantDefaults("Eliminar", PackIconKind.Delete, dangerColor, true);
                break;

            case ButtonVariant.Edit:
                SetVariantDefaults("Editar", PackIconKind.Pencil, infoColor, true);
                break;

            case ButtonVariant.Print:
                SetVariantDefaults("Imprimir", PackIconKind.Printer, null, false);
                if (ButtonStyle == null)
                {
                    ButtonStyle = Application.Current.TryFindResource("MaterialDesignOutlinedButton") as Style;
                }
                break;

            case ButtonVariant.Custom:
                // No aplicar defaults, dejar que el usuario configure todo
                break;
        }
    }

    private void SetVariantDefaults(string text, PackIconKind icon, Brush backgroundColor, bool isRaised)
    {
        // Solo aplicar texto si no se ha sobrescrito manualmente
        if (string.IsNullOrEmpty(Text) || Text == "Button")
            Text = text;

        // Siempre aplicar el ícono de la variante
        IconKind = icon;

        // Siempre aplicar el color de la variante
        if (backgroundColor != null)
            BackgroundColor = backgroundColor;

        if (ButtonStyle == null)
        {
            var styleName = isRaised ? "MaterialDesignRaisedButton" : "MaterialDesignOutlinedButton";
            ButtonStyle = Application.Current.TryFindResource(styleName) as Style;
        }
    }
}
