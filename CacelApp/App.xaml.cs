using CacelApp.Config;
using CacelApp.Modulos.Login;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace CacelApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
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

            base.OnStartup(e);
        }
        protected override async void OnExit(ExitEventArgs e)
        {
            // Detiene el Host (libera recursos)
            using (_host)
            {
                await _host.StopAsync();
            }
            base.OnExit(e);
        }
    }

}
