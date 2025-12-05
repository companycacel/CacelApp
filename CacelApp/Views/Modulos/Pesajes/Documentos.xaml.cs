namespace CacelApp.Views.Modulos.Pesajes;

public partial class Documentos : Window
{
    public DocumentosModel ViewModel { get; }

    public Documentos(DocumentosModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        // Asignar la acción de cierre
        ViewModel.RequestClose = () => DialogResult = ViewModel.DocumentoSeleccionado != null;

        Loaded += async (s, e) => await ViewModel.InicializarAsync();
    }
}
