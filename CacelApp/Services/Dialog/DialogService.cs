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

        // Check if the active window is a modal dialog (has Owner set)
        var activeWindow = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
            .FirstOrDefault(w => w.IsActive);

        // If active window is modal (ShowDialog), show dialog in a Topmost window
        if (activeWindow != null && activeWindow.Owner != null)
        {
            return await ShowDialogInTopmostWindow(config);
        }

        // Otherwise use the standard DialogHost
        var dispatcherOp = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var identifier = dialogIdentifier ?? "RootDialogHost";
            return await MaterialDesignThemes.Wpf.DialogHost.Show(config, identifier);
        }, DispatcherPriority.Normal);

        return await dispatcherOp.Task.Unwrap();
    }

    private async Task<object?> ShowDialogInTopmostWindow(DialogConfig config)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<object?>();

        await _dispatcher.InvokeAsync(() =>
        {
            // Get the active window to position the dialog over it
            var activeWindow = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive);

            var dialogWindow = new System.Windows.Window
            {
                WindowStyle = System.Windows.WindowStyle.None,
                ResizeMode = System.Windows.ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true,
                Topmost = true,
                ShowInTaskbar = false,
                Owner = activeWindow
            };

            // Set window size and position to match the active window
            if (activeWindow != null)
            {
                dialogWindow.Left = activeWindow.Left;
                dialogWindow.Top = activeWindow.Top;
                dialogWindow.Width = activeWindow.ActualWidth;
                dialogWindow.Height = activeWindow.ActualHeight;
            }
            else
            {
                dialogWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dialogWindow.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
                dialogWindow.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            }

            // Set foreground color from MaterialDesign theme
            try
            {
                var foregroundBrush = System.Windows.Application.Current.TryFindResource("MaterialDesignBody") as System.Windows.Media.Brush;
                if (foregroundBrush != null)
                {
                    System.Windows.Documents.TextElement.SetForeground(dialogWindow, foregroundBrush);
                }
            }
            catch { }

            // Create a grid to hold the overlay and dialog
            var grid = new System.Windows.Controls.Grid();

            // Add semi-transparent overlay
            var overlay = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(50, 0, 0, 0)) 
            };
            grid.Children.Add(overlay);

            // Create the dialog content using the template
            var contentControl = new System.Windows.Controls.ContentControl
            {
                Content = config,
                ContentTemplate = (System.Windows.DataTemplate)System.Windows.Application.Current.FindResource("AppDialogTemplate"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            grid.Children.Add(contentControl);

            dialogWindow.Content = grid;

            // Handle button clicks after content is loaded
            dialogWindow.ContentRendered += (s, e) =>
            {
                // Find all buttons in the visual tree
                var buttons = FindVisualChildren<System.Windows.Controls.Button>(dialogWindow).ToList();
                
                foreach (var button in buttons)
                {
                    // Remove the Command binding and use Click event instead
                    button.Command = null;
                    button.Click += (bs, be) =>
                    {
                        // Get the CommandParameter which contains the result (True/False)
                        var result = button.CommandParameter;
                        tcs.TrySetResult(result);
                        dialogWindow.Close();
                    };
                }
            };

            dialogWindow.Closed += (s, e) =>
            {
                // Ensure task completes even if window is closed without clicking a button
                tcs.TrySetResult(null);
            };

            dialogWindow.ShowDialog();
        });

        return await tcs.Task;
    }

    private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
    {
        if (depObj != null)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (var childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
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
