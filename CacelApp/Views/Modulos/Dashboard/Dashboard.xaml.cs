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
            System.Diagnostics.Debug.WriteLine("=== Dashboard_Loaded: Iniciando ===");
            System.Diagnostics.Debug.WriteLine($"CameraStreams count: {_viewModel.CameraStreams?.Count ?? 0}");

            // Esperar a que el ItemsControl se renderice
            await Dispatcher.InvokeAsync(async () =>
            {
                // Crear WindowsFormsHost y PictureBox para cada cámara
                var handlesPorCanal = new Dictionary<int, IntPtr>();

                // Buscar todos los Border containers en el ItemsControl
                var itemsControl = CameraStreamsControl;
                System.Diagnostics.Debug.WriteLine($"ItemsControl encontrado: {itemsControl != null}");

                if (itemsControl != null)
                {
                    // Esperar a que los items se generen
                    await Task.Delay(500);

                    System.Diagnostics.Debug.WriteLine($"Iterando sobre {_viewModel.CameraStreams.Count} cámaras");

                    foreach (var item in _viewModel.CameraStreams)
                    {
                        System.Diagnostics.Debug.WriteLine($"Procesando cámara: Canal={item.Canal}, Nombre={item.Nombre}");

                        var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                        if (container != null)
                        {
                            // Buscar el Border con nombre CameraHostContainer
                            var borderHost = FindVisualChild<Border>(container, "CameraHostContainer");
                            System.Diagnostics.Debug.WriteLine($"  Border encontrado: {borderHost != null}, Tag: {borderHost?.Tag}");

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

                                    System.Diagnostics.Debug.WriteLine($"  ✓ PictureBox creado para canal {canal}, Handle: {pictureBox.Handle}");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"  ✗ Error creating camera host for canal {canal}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  ✗ Container no encontrado para cámara {item.Nombre}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Total handles creados: {handlesPorCanal.Count}");

                    // Iniciar streaming con los handles
                    if (handlesPorCanal.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("Iniciando streaming...");
                        await _viewModel.IniciarStreamingCamarasAsync(handlesPorCanal);
                        System.Diagnostics.Debug.WriteLine("Streaming iniciado");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠ No se crearon handles, no se puede iniciar streaming");
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);

            System.Diagnostics.Debug.WriteLine("=== Dashboard_Loaded: Finalizado ===");
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