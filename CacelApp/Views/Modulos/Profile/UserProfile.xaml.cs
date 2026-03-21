using UserControl = System.Windows.Controls.UserControl;

namespace CacelApp.Views.Modulos.Profile;

public partial class UserProfile : UserControl
{
    public UserProfile()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // If opened as a dialog, close the dialog.
            if (MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.CanExecute(null, this))
            {
                MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(null, this);
                return;
            }

            // If hosted inside MainWindow content, navigate back to Dashboard.
            var main = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (main?.DataContext is MainWindowModel vm)
            {
                vm.NavigateToDashboard();
            }
        }
        catch { }
    }
}
