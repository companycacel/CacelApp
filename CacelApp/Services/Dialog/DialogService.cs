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

    public async Task ShowAlert(DialogConfig config)
    {
        (config.IconKind, config.AccentColor) = config.Type switch
        {
            AlertType.Success => (PackIconKind.CheckCircleOutline, Brushes.Green),
            AlertType.Error => (PackIconKind.AlertCircle, Brushes.Red),
            AlertType.Warning => (PackIconKind.AlertOutline, Brushes.Orange),
            _ => (PackIconKind.InformationOutline, Brushes.Blue)
        };

        await _dispatcher.InvokeAsync(() =>
        {
            DialogHost.Show(config, "RootDialogHost");
        });
    }

    public async Task<bool> ShowConfirm(string title, string message)
    {
        var config = new DialogConfig
        {
            Title = title,
            Message = message,
            Type = AlertType.Warning, // Usamos Warning para confirmaciones
            PrimaryText = "Confirmar",
            SecondaryText = "Cancelar",
        };


        await ShowAlert(config);
        return false; // Retorno temporal hasta implementar la captura de resultado.
    }

    public async Task ShowError(string message, string title = "Error Crítico", string primaryText = "Aceptar", string details = null)
    {
        var config = new DialogConfig
        {
            Title = "Error Crítico", // Valor por defecto
            Message = message,
            Type = AlertType.Error,
            PrimaryText = "Aceptar", // Botón por defecto
            SecondaryText = null // Sin botón secundario
        };
        await ShowAlert(config);
    }

    public async Task ShowInfo(string message, string title = "Información del Sistema", string primaryText = "Aceptar", string details = null)
    {
        var config = new DialogConfig
        {
            Title = "Información del Sistema",
            Message = message,
            Type = AlertType.Info,
            PrimaryText = "Aceptar"
        };
        await ShowAlert(config);
    }

    public async Task ShowSuccess(string message, string title = "Operación Exitosa", string primaryText = "Continuar", string details = null)
    {
        var config = new DialogConfig
        {
            Title = "Operación Exitosa",
            Message = message,
            Type = AlertType.Success,
            PrimaryText = "Continuar"
        };
        await ShowAlert(config);
    }

    public async Task ShowWarning(string message, string title = "Advertencia", string primaryText = "Entendido", string details = null)
    {
        var config = new DialogConfig
        {
            Title = "Advertencia",
            Message = message,
            Type = AlertType.Warning,
            PrimaryText = "Entendido"
        };
        await ShowAlert(config);
    }
}
