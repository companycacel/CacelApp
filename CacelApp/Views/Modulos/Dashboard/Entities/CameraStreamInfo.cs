using CommunityToolkit.Mvvm.ComponentModel;

namespace CacelApp.Views.Modulos.Dashboard.Entities;

/// <summary>
/// Información de stream de cámara para el Dashboard
/// </summary>
public partial class CameraStreamInfo : ObservableObject
{
    [ObservableProperty]
    private int _canal;

    [ObservableProperty]
    private string _nombre = "";

    [ObservableProperty]
    private string _ubicacion = "";

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private IntPtr _streamHandle = IntPtr.Zero;

    [ObservableProperty]
    private IntPtr _handleVentana = IntPtr.Zero;

    [ObservableProperty]
    private bool _isSelected;
}
