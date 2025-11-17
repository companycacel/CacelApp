using System.Windows;
using System.Windows.Input;

namespace CacelApp.Shared.Controls;

/// <summary>
/// Ventana para visualizar imágenes con zoom y navegación
/// Componente reutilizable para mostrar imágenes de balanza u otros módulos
/// </summary>
public partial class ImageViewerWindow : Window
{
    public ImageViewerWindow(ImageViewerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Configurar eventos
        viewModel.CerrarVentanaAction = () => this.Close();
        viewModel.ToggleFullscreenAction = ToggleFullscreen;
    }

    private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && DataContext is ImageViewerViewModel vm)
        {
            // Doble click para zoom o pantalla completa
            vm.ToggleFullscreenCommand.Execute(null);
        }
    }

    private void ImagenGrilla_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is System.Windows.Media.Imaging.BitmapImage imagen)
        {
            if (DataContext is ImageViewerViewModel vm)
            {
                vm.SeleccionarImagen(imagen);
            }
        }
    }

    private void ToggleFullscreen()
    {
        if (WindowState == WindowState.Normal)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }
}
