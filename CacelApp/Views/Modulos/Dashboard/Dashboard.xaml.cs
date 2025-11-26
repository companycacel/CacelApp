using System.Windows.Forms.Integration;
using System.Collections.Generic;
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

        public Dashboard(DashboardModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            Loaded += Dashboard_Loaded;
            Unloaded += Dashboard_Unloaded;
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
                    if (handlesPorCanal.Any())
                    {
                        await _viewModel.IniciarStreamingCamarasAsync(handlesPorCanal);
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
        private void Dashboard_Unloaded(object sender, RoutedEventArgs e)
        {
            // Limpiar PictureBoxes
            foreach (var pictureBox in _pictureBoxes.Values)
            {
                pictureBox?.Dispose();
            }
            _pictureBoxes.Clear();
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