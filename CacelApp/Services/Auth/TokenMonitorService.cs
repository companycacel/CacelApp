
using CacelApp.Services.Dialog;
using CacelApp.Views.Modulos.Login;
using Core.Repositories.Login;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;
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
        StopMonitoring();

        try
        {
            // Si el usuario no responde en WarningTime (2 min), procedemos al logout automático.
            var promptTask = ShowRefreshPrompt();
            var timeoutTask = Task.Delay(WarningTime);

            var completedTask = await Task.WhenAny(promptTask, timeoutTask);

            if (completedTask == promptTask)
            {
                bool wantsToContinue = await promptTask;
                if (wantsToContinue)
                {
                    await AttemptTokenRefresh();
                }
                else
                {
                    PerformLogoutAndReturnToLogin("Sesión cerrada por elección del usuario.");
                }
            }
            else
            {
                PerformLogoutAndReturnToLogin("La sesión ha expirado por inactividad.");
            }
        }
        catch (Exception)
        {
            PerformLogoutAndReturnToLogin("Se cerró la sesión por seguridad.");
        }
    }

    private async Task<bool> ShowRefreshPrompt()
    {
        return await _dialogService.ShowConfirm(
            $"Su sesión expirará pronto. ¿Desea extenderla por seguridad?",
            "Sesión a punto de expirar",
            "Continuar Sesión",
            "Cerrar Sesión"
        );
    }

    // Lógica para refrescar el token
    private async Task AttemptTokenRefresh()
    {
        try
        {
            var response = await _authService.RefreshTokenAsync();
            StartMonitoring(response.Data.ExpiresAt);
            await _dialogService.ShowSuccess("Sesión extendida.", title: "Refresco Exitoso");
        }
        catch (Exception)
        {
            PerformLogoutAndReturnToLogin("Su sesión ha caducado en el servidor. Por favor, ingrese nuevamente.");
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
            await _dialogService.ShowInfo(reason, title: "Sesión Finalizada", primaryText: "Ir a Login");

            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow?.Close();
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
