using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace CacelApp.Shared.Controls.PdfViewer;

/// <summary>
/// Ventana para previsualizar documentos PDF con opciones de guardar e imprimir
/// Utiliza WebView2 para mostrar el documento PDF directamente en la ventana
/// </summary>
public partial class PdfViewerWindow : Window
{
    private readonly PdfViewerViewModel _viewModel;
    private readonly byte[] _pdfBytes;

    public PdfViewerWindow(byte[] pdfBytes, string titulo = "Documento PDF")
    {
        InitializeComponent();

        _pdfBytes = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        _viewModel = new PdfViewerViewModel(this, pdfBytes, titulo);
        DataContext = _viewModel;

        Loaded += PdfViewerWindow_Loaded;
        Closed += PdfViewerWindow_Closed;
    }

    private async void PdfViewerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.CargarPdfEnWebViewAsync(PdfWebView);
    }

    private void PdfViewerWindow_Closed(object? sender, EventArgs e)
    {
        // Limpiar archivo temporal del ViewModel
        _viewModel.LimpiarArchivoTemporal();
    }
}

/// <summary>
/// ViewModel para el visor de PDF
/// </summary>
public partial class PdfViewerViewModel : ObservableObject
{
    private readonly Window _window;
    private readonly byte[] _pdfBytes;
    private string? _tempPdfPath;

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private bool isDocumentLoaded;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string? errorMessage;

    public IRelayCommand GuardarCommand { get; }
    public IRelayCommand ImprimirCommand { get; }
    public IRelayCommand CerrarCommand { get; }

    public PdfViewerViewModel(Window window, byte[] pdfBytes, string titulo)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _pdfBytes = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        title = titulo;

        GuardarCommand = new RelayCommand(GuardarPdf);
        ImprimirCommand = new RelayCommand(ImprimirPdf);
        CerrarCommand = new RelayCommand(() => _window.Close());
    }

    /// <summary>
    /// Carga el PDF en el control WebView2
    /// </summary>
    public async Task CargarPdfEnWebViewAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
    {
        string tempPath = string.Empty;

        try
        {
            IsLoading = true;
            HasError = false;

            // Inicializar WebView2
            await webView.EnsureCoreWebView2Async(null);

            await Task.Run(() =>
            {
                // Crear archivo temporal
                tempPath = Path.Combine(Path.GetTempPath(), $"{Title}_{Guid.NewGuid()}.pdf");
                File.WriteAllBytes(tempPath, _pdfBytes);
            });

            // Guardar referencia al archivo temporal para limpiarlo después
            _tempPdfPath = tempPath;

            // Navegar al PDF
            webView.Source = new Uri(tempPath);

            IsDocumentLoaded = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"No se pudo cargar el PDF: {ex.Message}";

            _window.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    _window,
                    $"Error al cargar el PDF: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Guarda el PDF en una ubicación seleccionada por el usuario
    /// </summary>
    private void GuardarPdf()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                FileName = $"{Title}.pdf",
                DefaultExt = ".pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _pdfBytes);
                MessageBox.Show(
                    _window,
                    "El archivo PDF se guardó correctamente.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                _window,
                $"Error al guardar el archivo: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Imprime el documento PDF abriendo el cuadro de diálogo de impresión del sistema
    /// </summary>
    private void ImprimirPdf()
    {
        try
        {
            // Guardar temporalmente y abrir con el visor para imprimir
            var tempPath = Path.Combine(Path.GetTempPath(), $"{Title}_print_{Guid.NewGuid()}.pdf");
            File.WriteAllBytes(tempPath, _pdfBytes);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true,
                Verb = "print" // Intenta abrir directamente el diálogo de impresión
            };

            Process.Start(processStartInfo);

            MessageBox.Show(
                _window,
                "Se ha enviado el documento a imprimir.\n" +
                "Si no se abre el diálogo de impresión automáticamente, " +
                "puede imprimir desde el visor de PDF que se abrió.",
                "Imprimir",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Programar eliminación del archivo temporal después de un delay
            Task.Delay(10000).ContinueWith(_ =>
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch { }
            });
        }
        catch (Exception ex)
        {

            MessageBox.Show(
                _window,
                $"Error al imprimir: {ex.Message}\n\n" +
                "Puede usar el botón 'Guardar' para guardar el PDF y luego imprimirlo manualmente.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Limpia el archivo temporal del PDF
    /// </summary>
    public void LimpiarArchivoTemporal()
    {
        if (!string.IsNullOrEmpty(_tempPdfPath) && File.Exists(_tempPdfPath))
        {
            try
            {
                File.Delete(_tempPdfPath);
            }
            catch
            {
                // Ignorar errores al eliminar temporal
            }
        }
    }
}
