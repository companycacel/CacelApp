using MaterialDesignThemes.Wpf;

namespace CacelApp.Shared.Controls.Form;

public partial class ModalWindow : Window
{
    public static readonly DependencyProperty HeaderIconKindProperty =
        DependencyProperty.Register(nameof(HeaderIconKind), typeof(PackIconKind), typeof(ModalWindow),
            new PropertyMetadata(PackIconKind.WindowMaximize, OnHeaderIconKindChanged));

    public static readonly DependencyProperty HeaderTitleTextProperty =
        DependencyProperty.Register(nameof(HeaderTitleText), typeof(string), typeof(ModalWindow),
            new PropertyMetadata(string.Empty, OnHeaderTitleTextChanged));

    public static readonly DependencyProperty HeaderSubtitleTextProperty =
        DependencyProperty.Register(nameof(HeaderSubtitleText), typeof(string), typeof(ModalWindow),
            new PropertyMetadata(string.Empty, OnHeaderSubtitleTextChanged));

    public static readonly DependencyProperty HeaderExtraProperty =
        DependencyProperty.Register(nameof(HeaderExtra), typeof(object), typeof(ModalWindow),
            new PropertyMetadata(null, OnHeaderExtraChanged));

    public static readonly DependencyProperty MainContentProperty =
        DependencyProperty.Register(nameof(MainContentValue), typeof(object), typeof(ModalWindow),
            new PropertyMetadata(null, OnMainContentChanged));

    public static readonly DependencyProperty FooterContentProperty =
        DependencyProperty.Register(nameof(FooterContentValue), typeof(object), typeof(ModalWindow),
            new PropertyMetadata(null, OnFooterContentChanged));

    public PackIconKind HeaderIconKind
    {
        get => (PackIconKind)GetValue(HeaderIconKindProperty);
        set => SetValue(HeaderIconKindProperty, value);
    }

    public string HeaderTitleText
    {
        get => (string)GetValue(HeaderTitleTextProperty);
        set => SetValue(HeaderTitleTextProperty, value);
    }

    public string HeaderSubtitleText
    {
        get => (string)GetValue(HeaderSubtitleTextProperty);
        set => SetValue(HeaderSubtitleTextProperty, value);
    }

    public object HeaderExtra
    {
        get => GetValue(HeaderExtraProperty);
        set => SetValue(HeaderExtraProperty, value);
    }

    public object MainContentValue
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public object FooterContentValue
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    public ModalWindow()
    {
        InitializeComponent();
    }

    private static void OnHeaderIconKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalWindow window)
            window.HeaderIcon.Kind = (PackIconKind)e.NewValue;
    }

    private static void OnHeaderTitleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalWindow window)
            window.HeaderTitle.Text = e.NewValue?.ToString() ?? string.Empty;
    }

    private static void OnHeaderSubtitleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalWindow window)
        {
            window.HeaderSubtitle.Text = e.NewValue?.ToString() ?? string.Empty;
            window.HeaderSubtitle.Visibility = string.IsNullOrEmpty(e.NewValue?.ToString())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private static void OnHeaderExtraChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalWindow window)
            window.HeaderExtraContent.Content = e.NewValue;
    }

    private static void OnMainContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalWindow window)
            window.MainContent.Content = e.NewValue;
    }

    private static void OnFooterContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalWindow window)
            window.FooterContent.Content = e.NewValue;
    }
}
