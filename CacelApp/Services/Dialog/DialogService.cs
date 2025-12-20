using CacelApp.Services.Loading;
using CacelApp.Shared.Entities;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;

namespace CacelApp.Services.Dialog;

public class DialogService : IDialogService
{
    private readonly Dispatcher _dispatcher;
    private readonly ILoadingService? _loadingService;

    public DialogService(ILoadingService? loadingService = null)
    {
        _dispatcher = System.Windows.Application.Current.Dispatcher;
        _loadingService = loadingService;
    }

    public async Task<object?> ShowAlert(DialogConfig config, string? dialogIdentifier = null)
    {
        (config.IconKind, config.AccentColor) = config.Type switch
        {
            AlertType.Success => (PackIconKind.CheckCircleOutline, Brushes.Green),
            AlertType.Error => (PackIconKind.AlertCircle, Brushes.Red),
            AlertType.Warning => (PackIconKind.AlertOutline, Brushes.Orange),
            _ => (PackIconKind.InformationOutline, Brushes.Blue)
        };

        try
        {
            _loadingService?.StopLoading();
        }
        catch
        {
        }

        // Determine which DialogHost to use based on active window
        var dispatcherOp = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            string identifier;

            if (dialogIdentifier != null)
            {
                // If explicitly specified, use it
                identifier = dialogIdentifier;
            }
            else
            {
                // Auto-detect based on active window
                var activeWindow = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive);

                // If no active window, try to find the focused window
                if (activeWindow == null)
                {
                    activeWindow = System.Windows.Application.Current.Windows
                        .OfType<System.Windows.Window>()
                        .FirstOrDefault(w => w.IsFocused);
                }

                // If still null, use MainWindow
                if (activeWindow == null)
                {
                    activeWindow = System.Windows.Application.Current.MainWindow;
                }

                // Determine DialogHost identifier based on window type
                identifier = activeWindow?.GetType().Name switch
                {
                    "MantPesajes" => "MantPesajesDialogHost",
                    "MantProduccion" => "MantProduccionDialogHost",
                    "RegistroRapidoProduccion" => "RegistroRapidoProduccionDialogHost",
                    "MantBalanza" => "MantBalanzaDialogHost",
                    _ => "RootDialogHost"
                };
            }

            // Activate the window to ensure dialog appears on top
            var targetWindow = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive || w.IsFocused)
                ?? System.Windows.Application.Current.MainWindow;

            if (targetWindow != null)
            {
                targetWindow.Activate();
            }

            return await MaterialDesignThemes.Wpf.DialogHost.Show(config, identifier);
        }, DispatcherPriority.Send); // Changed from Normal to Send for highest priority

        return await dispatcherOp.Task.Unwrap();
    }


    public async Task<bool> ShowConfirm(string message, string? title = null, string? primaryText = null, string? secondaryText = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Confirmación",
            Message = message,
            Type = AlertType.Warning,
            PrimaryText = primaryText ?? "Aceptar",
            SecondaryText = secondaryText ?? "Cancelar"
        };

        object? result = await ShowAlert(config, dialogIdentifier);
        if (result is bool boolResult)
        {
            return boolResult;
        }

        string? resultString = result?.ToString();
        bool finalResult = resultString?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
        return finalResult;
    }

    public async Task ShowError(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Error Crítico",
            Message = message,
            Type = AlertType.Error,
            SecondaryText = null
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config, dialogIdentifier);
    }

    public async Task ShowInfo(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Información del Sistema",
            Message = message,
            Type = AlertType.Info,
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config, dialogIdentifier);
    }

    public async Task ShowSuccess(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Operación Exitosa",
            Message = message,
            Type = AlertType.Success,
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config, dialogIdentifier);
    }

    public async Task ShowWarning(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Advertencia",
            Message = message,
            Type = AlertType.Warning,
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config, dialogIdentifier);
    }
}
