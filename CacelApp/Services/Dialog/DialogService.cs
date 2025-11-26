using CacelApp.Shared.Entities;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;

namespace CacelApp.Services.Dialog;

public class DialogService : IDialogService
{
    private readonly Dispatcher _dispatcher;

    public DialogService()
    {
        _dispatcher = System.Windows.Application.Current.Dispatcher;
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








        var dispatcherOp = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            var identifier = dialogIdentifier ?? "RootDialogHost";
            return await MaterialDesignThemes.Wpf.DialogHost.Show(config, identifier);
        }, DispatcherPriority.Render);

        return await dispatcherOp.Task.Unwrap();
    }

    public async Task<bool> ShowConfirm(string title, string message, string? primaryText = null, string? secondaryText = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title,
            Message = message,
            Type = AlertType.Warning, // Usamos Warning para confirmaciones
            PrimaryText = primaryText ?? "Aceptar",
            SecondaryText = secondaryText ?? "Cancelar" // Importante: establecer Cancelar por defecto
        };

        object? result = await ShowAlert(config, dialogIdentifier);
        if (result is bool boolResult)
        {
            return boolResult;
        }

        // Intentar convertir a string
        string? resultString = result?.ToString();
        bool finalResult = resultString?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
        return finalResult;
    }

    public async Task ShowError(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null)
    {
        var config = new DialogConfig
        {
            Title = title ?? "Error Crítico", // Valor por defecto
            Message = message,
            Type = AlertType.Error,
            SecondaryText = null // Sin botón secundario
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
