using CacelApp.Services.Auth;
using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Views.Modulos.Balanza;
using CacelApp.Views.Modulos.Dashboard;
using CacelApp.Views.Modulos.Login;
using Core.Repositories.Balanza;
using Core.Repositories.Login;
using Core.Repositories.Profile;
using Infrastructure.WebApi.Repositories.Balanza;
using Infrastructure.Services.Balanza;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace CacelApp.Config
{
    public static class DependencyInjection
    {
        private static readonly Uri BaseApiUri = new Uri(AppConfiguration.Api.BaseUrl);
        
        public static IServiceCollection RegisterAllServices(this IServiceCollection services)
        {
            RegisterPresentationServices(services);
            RegisterApplicationServices(services);
            RegisterRepositoryServices(services);
   
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
            services.AddTransient<Views.Modulos.Profile.UserProfile>();

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

            services.AddScoped<IBalanzaReadService, BalanzaReadService>();
            services.AddScoped<IBalanzaWriteService, BalanzaWriteService>();
            services.AddScoped<IBalanzaReportService, BalanzaReportService>();

  
        }
        private static void RegisterRepositoryServices(IServiceCollection services)
        {
            services.AddScoped<IBalanzaReadRepository, BalanzaReadRepository>();
            services.AddScoped<IBalanzaWriteRepository, BalanzaWriteRepository>();
            services.AddScoped<IBalanzaReportRepository, BalanzaReportRepository>();
        }

    }
}
