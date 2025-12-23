namespace CacelApp.Views.Modulos.Pesajes;

public partial class MantPesajes : Window
{
    public MantPesajes()
    {
        InitializeComponent();

        // Conectar el RequestClose del ViewModel con el método Close de la ventana
        Loaded += (s, e) =>
        {
            if (DataContext is MantPesajesModel viewModel)
            {
                viewModel.RequestClose = (success) =>
                {
                    if (success) this.DialogResult = true;
                    this.Close();
                };

                viewModel.ScrollToEditPanel += async () =>
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    
                    var editPanel = this.FindName("EditPanel") as System.Windows.FrameworkElement;
                    if (editPanel != null)
                    {
                        editPanel.BringIntoView();
                    }
                };
            }
        };

        // Verificar datos pendientes antes de cerrar
        Closing += async (s, e) =>
        {
            if (DataContext is MantPesajesModel viewModel)
            {
                if (viewModel.HasPendingDetails)
                {
                    e.Cancel = true; // Cancelar el cierre temporalmente

                    var result = await viewModel.DialogService.ShowConfirm(
                        "Tiene cambios sin guardar en los detalles. ¿Desea salir sin guardar?",
                        "Datos pendientes");

                    if (result)
                    {
                        Closing -= null;
                        this.Close();
                    }
                }
            }
        };

        // Limpiar recursos al cerrar
        Closed += (s, e) =>
        {
            if (DataContext is MantPesajesModel viewModel)
            {
                viewModel.Cleanup();
            }
        };
    }

    private System.Windows.Controls.ScrollViewer? FindScrollViewer(System.Windows.DependencyObject obj)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
            if (child is System.Windows.Controls.ScrollViewer scrollViewer)
                return scrollViewer;

            var result = FindScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
