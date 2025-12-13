using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace CacelApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Flag para saber si el cierre es por logout o por el usuario cerrando con X
        public bool IsLogoutInProgress { get; set; } = false;

        public MainWindow(MainWindowModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Solo cerrar la app si el usuario cerró con X (no por logout)
            if (!IsLogoutInProgress)
            {
                Application.Current.Shutdown();
            }
        }

        private void UserMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}