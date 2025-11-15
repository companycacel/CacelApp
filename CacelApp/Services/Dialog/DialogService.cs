using CacelApp.Shared.Entities;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using System.Windows.Threading;

namespace CacelApp.Services.Dialog;

public class DialogService : IDialogService
{
    private readonly Dispatcher _dispatcher;

    public DialogService()
    {
        _dispatcher = System.Windows.Application.Current.Dispatcher;
    }

    public async Task<object?> ShowAlert(DialogConfig config)
    {
        (config.IconKind, config.AccentColor) = config.Type switch
        {
            AlertType.Success => (PackIconKind.CheckCircleOutline, Brushes.Green),
            AlertType.Error => (PackIconKind.AlertCircle, Brushes.Red),
            AlertType.Warning => (PackIconKind.AlertOutline, Brushes.Orange),
            _ => (PackIconKind.InformationOutline, Brushes.Blue)
        };
        return await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            return await MaterialDesignThemes.Wpf.DialogHost.Show(config, "RootDialogHost");
        }, DispatcherPriority.Render);
    }

    public async Task<bool> ShowConfirm(string title, string message, string? primaryText = null, string? secondaryText = null)
    {
        var config = new DialogConfig
        {
            Title = title,
            Message = message,
            Type = AlertType.Warning, // Usamos Warning para confirmaciones
        };

        if (primaryText != null) config.PrimaryText = primaryText;
        if (secondaryText != null) config.SecondaryText = secondaryText;


        object? result = await ShowAlert(config);
        return result?.ToString()?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public async Task ShowError(string message, string? title = null, string? primaryText = null, string? details = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Error Crítico", // Valor por defecto
            Message = message,
            Type = AlertType.Error,
            SecondaryText = null // Sin botón secundario
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config);
    }

    public async Task ShowInfo(string message, string? title = null, string? primaryText = null, string? details = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Información del Sistema",
            Message = message,
            Type = AlertType.Info,
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config);
    }

    public async Task ShowSuccess(string message, string? title = null, string? primaryText = null, string? details = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Operación Exitosa",
            Message = message,
            Type = AlertType.Success,
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config);
    }

    public async Task ShowWarning(string message, string? title = null, string? primaryText = null, string? details = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Advertencia",
            Message = message,
            Type = AlertType.Warning,
        };
        if (primaryText != null) config.PrimaryText = primaryText;
        await ShowAlert(config);
    }
}
