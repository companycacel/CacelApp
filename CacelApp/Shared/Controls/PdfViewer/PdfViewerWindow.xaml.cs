using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using MessageBox = System.Windows.MessageBox;

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


    public PdfViewerViewModel(Window window, byte[] pdfBytes, string titulo)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _pdfBytes = pdfBytes ?? throw new ArgumentNullException(nameof(pdfBytes));
        title = titulo;
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
            await webView.EnsureCoreWebView2Async(null);

            await Task.Run(() =>
            {
                tempPath = Path.Combine(Path.GetTempPath(), $"{Title}_{Guid.NewGuid()}.pdf");
                File.WriteAllBytes(tempPath, _pdfBytes);
            });

            _tempPdfPath = tempPath;

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
            }
        }
    }
}
