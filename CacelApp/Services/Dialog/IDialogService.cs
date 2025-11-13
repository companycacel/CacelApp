using CacelApp.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacelApp.Services.Dialog;

public interface IDialogService
{

    Task ShowError(string message, string title = "Error Crítico", string primaryText = "Aceptar", string details = null);
    Task ShowSuccess(string message, string title = "Operación Exitosa", string primaryText = "Continuar", string details = null);
    Task ShowWarning(string message, string title = "Advertencia", string primaryText = "Entendido", string details = null);
    Task ShowInfo(string message, string title = "Información del Sistema", string primaryText = "Aceptar", string details = null);

    // Método base
    Task ShowAlert(DialogConfig config);

    Task<bool> ShowConfirm(string title, string message);
}
