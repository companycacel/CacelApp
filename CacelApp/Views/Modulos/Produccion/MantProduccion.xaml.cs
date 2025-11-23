using System.Windows;

namespace CacelApp.Views.Modulos.Produccion
{
    public partial class MantProduccion : Window
    {
        public MantProduccion()
        {
            InitializeComponent();
            
            // Conectar el RequestClose del ViewModel con el método Close de la ventana
            Loaded += (s, e) =>
            {
                if (DataContext is MantProduccionModel viewModel)
                {
                    viewModel.RequestClose = () => this.Close();
                }
            };
        }

        public MantProduccion(MantProduccionModel viewModel) : this()
        {
            if (viewModel == null)
                throw new System.ArgumentNullException(nameof(viewModel));

            DataContext = viewModel;
        }
    }
}
