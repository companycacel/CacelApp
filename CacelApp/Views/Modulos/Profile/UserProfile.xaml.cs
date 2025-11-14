using System.Windows;
using System.Windows.Controls;

namespace CacelApp.Views.Modulos.Profile;

public partial class UserProfile : UserControl
{
    public UserProfile()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        // If this view is hosted inside MainWindow's content area, navigate back to Dashboard
        try
        {
            var main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (main?.DataContext is MainWindowModel vm)
            {
                vm.NavigateToDashboard();
            }
            else
            {
                // fallback: close any dialog host
                MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(null, this);
            }
        }
        catch { }
    }
}
