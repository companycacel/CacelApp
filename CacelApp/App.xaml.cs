using CacelApp.Config;
using CacelApp.Services.Dialog;
using CacelApp.Views.Modulos.Login;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace CacelApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private readonly IHost _host;
        public App()
        {
            // 1. Configuración del Host (Registro de dependencias)
            _host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>{ 
                services.RegisterAllServices();
            }).Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            // Resuelve la ventana de Login desde el contenedor y la muestra
            var loginWindow = _host.Services.GetRequiredService<Login>();
            loginWindow.Show();
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
        }
        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }
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
                // Ejecutar de forma asíncrona en el hilo de UI para evitar bloqueos/deadlocks
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
                        // Si falló mostrar el diálogo, mostrar MessageBox como último recurso
                        System.Windows.MessageBox.Show(inner?.Message ?? ex?.Message ?? "Error desconocido", "Error Fatal de la Aplicación");
                    }
                });
            }
            else
            {
                // No hay servicio de diálogo registrado: usar MessageBox
                System.Windows.MessageBox.Show(ex?.Message ?? "Error desconocido", "Error Fatal de la Aplicación");
            }
        }
    }

}
