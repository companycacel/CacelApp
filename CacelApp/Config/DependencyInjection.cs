using CacelApp.Services.Auth;
using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Views.Modulos.Balanza;
using CacelApp.Views.Modulos.Dashboard;
using CacelApp.Views.Modulos.Login;
using Core.Repositories.Login;
using Core.Repositories.Profile;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace CacelApp.Config
{
    public static class DependencyInjection
    {
        private static readonly Uri BaseApiUri = new Uri("http://38.253.154.34:3001/");
        public static IServiceCollection RegisterAllServices(this IServiceCollection services)
        {
            RegisterPresentationServices(services);
            RegisterApplicationServices(services);
            return services;
        }

        private static void RegisterPresentationServices(IServiceCollection services)
        {
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<ILoadingService, LoadingService>();
            services.AddSingleton<ITokenMonitorService, TokenMonitorService>();

            services.AddTransient<Login>();
            services.AddTransient<LoginModel>();

            /// <summary>
            /// Lo hacemos Singleton para gestionar la navegación central
            /// </summary> 
            services.AddSingleton<MainWindow>();         
            services.AddSingleton<MainWindowModel>(); 

            services.AddTransient<Dashboard>();
            services.AddTransient<DashboardModel>();

            services.AddTransient<Balanza>();
            services.AddTransient<BalanzaModel>();
        }

        private static void RegisterApplicationServices(IServiceCollection services)
        {
            services.AddHttpClient("BaseApiHttpClient", client =>{ client.BaseAddress = BaseApiUri;});
            services.AddSingleton<IAuthService, AuthService>(serviceProvider =>
            {
                var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var baseClient = factory.CreateClient("BaseApiHttpClient");
                return new AuthService(baseClient);
            });

            services.AddSingleton<IUserProfileService, UserProfileService>(serviceProvider =>
            {
                var authService = serviceProvider.GetRequiredService<IAuthService>();
                return new UserProfileService(authService);
            });

            // services.AddScoped<IMigrationService, MigrationService>(); 
            // services.AddScoped<ISelectOptionService, SelectOptionService>(); 
            // services.AddScoped<IWebSocketClient, WebSocketClient>(); 
        }

      
    }
}
