using CacelApp.Views.Modulos.Dashboard;
using CacelApp.Views.Modulos.Login;
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
            services.AddTransient<Login>();
            services.AddTransient<LoginModel>();


            services.AddSingleton<MainWindow>(); // El Shell de la aplicación           
            services.AddSingleton<MainWindowModel>(); // Lo hacemos Singleton para gestionar la navegación central

            services.AddTransient<Dashboard>();
            services.AddTransient<DashboardModel>();

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
