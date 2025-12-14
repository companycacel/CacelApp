using System.Collections.ObjectModel;

namespace CacelApp.Shared.Controls.WeightDisplay;

/// <summary>
/// Panel contenedor que renderiza dinámicamente displays de balanzas
/// según la configuración
/// </summary>
public partial class WeightDisplaysPanel : System.Windows.Controls.UserControl
{
    public WeightDisplaysPanel()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>
    /// Colección de información de balanzas a mostrar
    /// </summary>
    public static readonly DependencyProperty BalanzasInfoProperty =
        DependencyProperty.Register(
            nameof(BalanzasInfo),
            typeof(ObservableCollection<BalanzaDisplayInfo>),
            typeof(WeightDisplaysPanel),
            new PropertyMetadata(null));

    public ObservableCollection<BalanzaDisplayInfo>? BalanzasInfo
    {
        get => (ObservableCollection<BalanzaDisplayInfo>?)GetValue(BalanzasInfoProperty);
        set => SetValue(BalanzasInfoProperty, value);
    }

    #endregion
}
