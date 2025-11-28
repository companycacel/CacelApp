using System.Windows.Forms.Integration;
using UserControl = System.Windows.Controls.UserControl;


namespace CacelApp.Views.Modulos.Dashboard
{
    /// <summary>
    /// Lógica de interacción para Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl
    {
        private readonly DashboardModel _viewModel;
        private readonly Dictionary<int, System.Windows.Forms.PictureBox> _pictureBoxes = new();
        private System.Windows.Forms.PictureBox? _pictureBoxAmpliado;
        private WindowsFormsHost? _hostAmpliado;

        public Dashboard(DashboardModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // Suscribirse a cambios de cámara seleccionada
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            Loaded += Dashboard_Loaded;
            Unloaded += Dashboard_Unloaded;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DashboardModel.CamaraSeleccionada))
            {
                Dispatcher.InvokeAsync(() => ActualizarVistaAmpliada());
            }
        }

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Esperar a que el ItemsControl se renderice
            await Dispatcher.InvokeAsync(async () =>
            {
                // Crear WindowsFormsHost y PictureBox para cada cámara
                var handlesPorCanal = new Dictionary<int, IntPtr>();

                // Buscar todos los Border containers en el ItemsControl
                var itemsControl = CameraStreamsControl;

                if (itemsControl != null)
                {
                    // Esperar a que los items se generen
                    await Task.Delay(500);

                    foreach (var item in _viewModel.CameraStreams)
                    {
                        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                        if (container != null)
                        {
                            // Buscar el Border con nombre CameraHostContainer
                            var borderHost = FindVisualChild<Border>(container, "CameraHostContainer");
                            if (borderHost != null && borderHost.Tag is int canal)
                            {
                                try
                                {
                                    // Crear WindowsFormsHost
                                    var host = new WindowsFormsHost();

                                    // Crear PictureBox
                                    var pictureBox = new System.Windows.Forms.PictureBox
                                    {
                                        Dock = System.Windows.Forms.DockStyle.Fill,
                                        BackColor = System.Drawing.Color.Black,
                                        SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
                                    };

                                    host.Child = pictureBox;
                                    borderHost.Child = host;
                                    _pictureBoxes[canal] = pictureBox;

                                    // Asegurarse de que el control tenga un handle
                                    pictureBox.CreateControl();
                                    handlesPorCanal[canal] = pictureBox.Handle;
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                    }

                    // Configurar el contenedor ampliado
                    ConfigurarContenedorAmpliado();

                    if (handlesPorCanal.Any())
                    {
                        await _viewModel.IniciarStreamingCamarasAsync(handlesPorCanal);
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ConfigurarContenedorAmpliado()
        {
            var containerAmpliado = CameraHostContainerAmpliado;
            if (containerAmpliado != null)
            {
                try
                {
                    // Crear WindowsFormsHost para vista ampliada
                    _hostAmpliado = new WindowsFormsHost();

                    // Crear PictureBox para vista ampliada
                    _pictureBoxAmpliado = new System.Windows.Forms.PictureBox
                    {
                        Dock = System.Windows.Forms.DockStyle.Fill,
                        BackColor = System.Drawing.Color.Black,
                        SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
                    };

                    _hostAmpliado.Child = _pictureBoxAmpliado;
                    containerAmpliado.Child = _hostAmpliado;

                    // Asegurarse de que el control tenga un handle
                    _pictureBoxAmpliado.CreateControl();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error configurando contenedor ampliado: {ex.Message}");
                }
            }
        }

        private void ActualizarVistaAmpliada()
        {
            if (_pictureBoxAmpliado == null || _viewModel.CamaraSeleccionada == null)
                return;

            try
            {
                var canal = _viewModel.CamaraSeleccionada.Canal;

                // Detener el streaming anterior si existe
                if (_viewModel.CamaraSeleccionada.StreamHandle != IntPtr.Zero)
                {
                    // El streaming ya está activo, solo necesitamos redirigir el handle
                    // Llamar al servicio de cámara para actualizar el handle de visualización
                    _viewModel.ActualizarHandleCamaraAmpliada(canal, _pictureBoxAmpliado.Handle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error actualizando vista ampliada: {ex.Message}");
            }
        }

        private void Dashboard_Unloaded(object sender, RoutedEventArgs e)
        {
            // Desuscribirse de eventos
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            // Limpiar PictureBoxes
            foreach (var pictureBox in _pictureBoxes.Values)
            {
                pictureBox?.Dispose();
            }
            _pictureBoxes.Clear();

            // Limpiar vista ampliada
            _pictureBoxAmpliado?.Dispose();
            _hostAmpliado?.Dispose();
        }

        // Helper para encontrar controles hijos en el árbol visual
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        // Helper para encontrar controles hijos por nombre
        private static T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}