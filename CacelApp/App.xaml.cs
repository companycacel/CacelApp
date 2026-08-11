using CacelApp.Config;
using CacelApp.Services.Dialog;
using CacelApp.Views.Modulos.Login;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CacelApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;
        private static Mutex _mutex = null;
        private const string AppGuid = "CacelApp-7B8E-4A1C-93D2-1F2B3C4D5E6F";

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public static void ReleaseSingleInstanceMutex()
        {
            try
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                }
            }
            catch { }
        }

        public App()
        {
            _host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                services.RegisterAllServices();
            }).Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, "Global\\" + AppGuid, out createdNew);

            if (!createdNew)
            {
                // Si la instancia ya existe, intentamos traerla al frente
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var processName = currentProcess.ProcessName;
                var otherProcesses = System.Diagnostics.Process.GetProcessesByName(processName)
                    .Where(p => p.Id != currentProcess.Id);

                foreach (var process in otherProcesses)
                {
                    IntPtr hWnd = process.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                        break;
                    }
                }

                // Cerrar esta instancia
                System.Windows.Application.Current.Shutdown();
                return;
            }

            // Integración de Velopack - debe ejecutarse ANTES de cualquier UI
            try
            {
                Velopack.VelopackApp.Build().Run();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Velopack no disponible: {ex.Message}");
            }

            await _host.StartAsync();

            // Configurar ShutdownMode para que la app no se cierre al cerrar ventanas
            // Solo se cerrará cuando se llame explícitamente a Application.Current.Shutdown()
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = _host.Services.GetRequiredService<Login>();
            loginWindow.Show();
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
        }
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {

                var tokenMonitor = _host.Services.GetService<Services.Auth.ITokenMonitorService>();
                tokenMonitor?.StopMonitoring();

                var cameraService = _host.Services.GetService<Core.Services.Configuration.ICameraService>();
                cameraService?.DetenerCompletamente();

                var serialPortService = _host.Services.GetService<Core.Services.Configuration.ISerialPortService>();
                serialPortService?.DetenerLectura();

                // Esperar un momento para asegurar que los recursos se liberen correctamente
                await Task.Delay(500);
            }
            catch (Exception ex)
            {

            }

            // 5. Detener el host de DI
            try
            {
                using (_host)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch { }

            // 6. Forzar garbage collection para liberar recursos
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            base.OnExit(e);
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            ShowGlobalError(exception);
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowGlobalError(e.Exception);
        }

        private void ShowGlobalError(Exception ex)
        {
            var dialogService = _host.Services.GetService<IDialogService>();

            if (dialogService != null)
            {
                this.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await dialogService.ShowError(
                            message: ex?.Message ?? "Error desconocido",
                            title: "Error Fatal de la Aplicación",
                            details: "Por favor, contacte a soporte técnico."
                        );
                    }
                    catch (Exception inner)
                    {
                        System.Windows.MessageBox.Show(inner?.Message ?? ex?.Message ?? "Error desconocido", "Error Fatal de la Aplicación");
                    }
                });
            }
            else
            {
                System.Windows.MessageBox.Show(ex?.Message ?? "Error desconocido", "Error Fatal de la Aplicación");
            }
        }
    }

}
