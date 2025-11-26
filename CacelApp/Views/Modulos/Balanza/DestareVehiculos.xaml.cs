namespace CacelApp.Views.Modulos.Balanza
{
    public partial class DestareVehiculos : Window
    {
        private readonly DestareVehiculosModel _viewModel;

        public DestareVehiculos(DestareVehiculosModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.CargarRegistrosAsync();

            // Cuando se selecciona un registro, cerrar el diálogo con OK
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DestareVehiculosModel.RegistroSeleccionado)
                    && _viewModel.RegistroSeleccionado != null)
                {
                    DialogResult = true;
                    Close();
                }
            };
        }

        public Core.Repositories.Balanza.Entities.Baz? RegistroSeleccionado => _viewModel.RegistroSeleccionado;
    }
}
