
using CacelApp.Modulos.Login;
using Microsoft.Extensions.DependencyInjection;

namespace CacelApp.Config
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection RegisterAllServices(this IServiceCollection services)
        {
            // A. Servicios de Presentación (Vistas y ViewModels)
            RegisterPresentationServices(services);

            // B. Servicios de Aplicación (Lógica de coordinación)
            RegisterApplicationServices(services);

            // C. Servicios de Infraestructura (Repositorios/APIs)
            RegisterInfrastructureServices(services);

            return services;
        }

        private static void RegisterPresentationServices(IServiceCollection services)
        {
            // Vistas (Ventanas Principales - Singleton, Modulares - Transient)
            services.AddTransient<Login>();
            services.AddSingleton<MainWindow>(); // El Shell de la aplicación

            // ViewModels (Transient para mantener el estado limpio por sesión/uso)
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<MainWindowViewModel>(); // Lo hacemos Singleton para gestionar la navegación central
            //services.AddTransient<BalanzaViewModel>();
            // ... otros ViewModels, e.g., PesajesViewModel, ProduccionViewModel.

            // Si usas UserControls como vistas, regístralos como Transient también.
            // services.AddTransient<BalanzaView>(); 
        }

        private static void RegisterApplicationServices(IServiceCollection services)
        {
            // Aquí van los servicios de la capa 'Application' (Tus archivos .cs en la carpeta 'Application')
            // Ejemplo:
            // services.AddScoped<IMigrationService, MigrationService>(); 
            // services.AddScoped<ISelectOptionService, SelectOptionService>(); 
            // services.AddScoped<IWebSocketClient, WebSocketClient>(); 
        }

        private static void RegisterInfrastructureServices(IServiceCollection services)
        {
            // Aquí van las implementaciones de repositorios y servicios externos de la capa 'Infrastructure'
            // Ejemplo de tus repositorios:
            // services.AddScoped<IBalanceRepository, BalanceRepository>(); 
            // services.AddScoped<IPesajesRepository, PesajesRepository>(); 
            // services.AddScoped<IMigrationRepository, MigrationRepository>(); 
        }
    }
}
