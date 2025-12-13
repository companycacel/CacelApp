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
}
