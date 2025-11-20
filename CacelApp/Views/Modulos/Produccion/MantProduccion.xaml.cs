using CacelApp.Views.Modulos.Balanza;
using System.Windows;
namespace CacelApp.Views.Modulos.Produccion
{
    public partial class MantProduccion : Window
    {
        public MantProduccion()
        {
            InitializeComponent();
        }
        public MantProduccion(MantProduccionModel viewModel) : this()
        {
            if (viewModel == null)
                throw new System.ArgumentNullException(nameof(viewModel));

            DataContext = viewModel;
            viewModel.SetWindow(this);

        
        }
    }
}
