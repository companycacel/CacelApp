
using CacelApp.Services.Dialog;
using CacelApp.Views.Modulos.Login;
using Core.Repositories.Login;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Application = System.Windows.Application;

namespace CacelApp.Services.Auth;

public class TokenMonitorService : ITokenMonitorService
{
    private readonly System.Timers.Timer _expirationTimer;
    private readonly IDialogService _dialogService;
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    // Tiempo antes de la expiración para mostrar la alerta: 2 minutos
    private static readonly TimeSpan WarningTime = TimeSpan.FromMinutes(2);

    public TokenMonitorService(IDialogService dialogService, IAuthService authService, IServiceProvider serviceProvider)
    {
        _dialogService = dialogService;
        _authService = authService;
        _serviceProvider = serviceProvider;

        _expirationTimer = new System.Timers.Timer();
        _expirationTimer.Elapsed += OnTimerElapsed;
        _expirationTimer.AutoReset = false; // Solo se dispara una vez
    }

    public void StartMonitoring(DateTime expirationTime)
    {
        StopMonitoring();

        TimeSpan timeUntilWarning = expirationTime.Subtract(DateTime.Now).Subtract(WarningTime);

        if (timeUntilWarning <= TimeSpan.Zero)
        {
            // Si el token casi expira, forzamos la alerta de inmediato (ej: 1 segundo).
            _expirationTimer.Interval = 1000;
        }
        else
        {
            // Configura el temporizador para que se active WarningTime antes de la expiración real
            _expirationTimer.Interval = timeUntilWarning.TotalMilliseconds;
        }

        _expirationTimer.Start();
    }

    public void StopMonitoring()
    {
        _expirationTimer.Stop();
    }

    // 💡 Lógica que se ejecuta al expirar el temporizador
    private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        StopMonitoring(); // Detenemos el timer para evitar reentrancia

        // 1. Mostrar el diálogo de confirmación
        bool wantsToContinue = await ShowRefreshPrompt();

        if (wantsToContinue)
        {
            await AttemptTokenRefresh();
        }
        else
        {
            // El usuario elige cerrar o la sesión ya expiró
            PerformLogoutAndReturnToLogin("Sesión cerrada por elección del usuario.");
        }
    }

    private async Task<bool> ShowRefreshPrompt()
    {
        // Nota: Este método asume que IDialogService.ShowConfirmAsync puede retornar bool
        // (lo cual requiere la correcta implementación del DialogHost en MainWindow.xaml.cs)
        return await _dialogService.ShowConfirm(
            title: "Sesión a punto de expirar",
            message: $"Su sesión expirará pronto. ¿Desea extenderla por seguridad?",
            primaryText: "Continuar Sesión",
            secondaryText: "Cerrar Sesión"
        );
    }

    // Lógica para refrescar el token
    private async Task AttemptTokenRefresh()
    {
        try
        {
            var response = await _authService.RefreshTokenAsync();

            // Si es exitoso, actualiza el estado y reinicia el monitor.
            // Usamos response.Data.ExpiresAt del cuerpo JSON para el nuevo tiempo.
            StartMonitoring(response.Data.ExpiresAt);
            await _dialogService.ShowSuccess("Sesión extendida.", title: "Refresco Exitoso");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowError("No se pudo refrescar la sesión.", title: "Error de Sesión");
            PerformLogoutAndReturnToLogin($"Fallo el refresco del token: {ex.Message}");
        }
    }

    // Cierra la sesión y regresa al Login
    private void PerformLogoutAndReturnToLogin(string reason)
    {
        _authService.LogoutAsync();
        StopMonitoring();

        // Usar el Dispatcher para garantizar que la UI se actualice en el hilo correcto
        Application.Current.Dispatcher.Invoke(async () =>
        {
            // 1. Mostrar una alerta informativa (opcional, reemplaza al MessageBox)
            await _dialogService.ShowInfo(reason, title: "Sesión Finalizada", primaryText: "Ir a Login");

            // 2. Busca el MainWindow y lo cierra
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow?.Close();

            // 3. 💡 Resuelve la nueva instancia de Login desde el contenedor de DI
            try
            {
                var loginWindow = _serviceProvider.GetRequiredService<Login>();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowError($"Error Fatal: No se pudo cargar la ventana de Login. {ex.Message}", title: "Error de Sistema");
                Application.Current.Shutdown();
            }
        });
    }
}
