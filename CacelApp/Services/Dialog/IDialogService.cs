using CacelApp.Shared.Entities;

namespace CacelApp.Services.Dialog;

public interface IDialogService
{

    Task ShowError(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null);
    Task ShowSuccess(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null);
    Task ShowWarning(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null);
    Task ShowInfo(string message, string? title = null, string? primaryText = null, string? details = null, string? dialogIdentifier = null);

    // Método base
    Task<object?> ShowAlert(DialogConfig config, string? dialogIdentifier = null);

    Task<bool> ShowConfirm(string title, string message, string? primaryText = null, string? secondaryText = null, string? dialogIdentifier = null);
}
