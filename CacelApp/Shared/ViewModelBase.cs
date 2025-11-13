using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Exceptions;

namespace CacelApp.Shared
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        protected readonly IDialogService DialogService;
        protected readonly ILoadingService LoadingService;

        public bool IsBusy => LoadingService?.IsLoading ?? false;
        public bool IsNotBusy => !IsBusy;
        protected ViewModelBase(IDialogService dialogService, ILoadingService loadingService)
        {
            DialogService = dialogService;
            LoadingService = loadingService;
            if (LoadingService != null)
            {
                // Suscribirse al evento para actualizar la UI cuando el estado cambia.
                LoadingService.LoadingStateChanged += OnLoadingStateChanged;
            }
        }
        protected ViewModelBase():this(null, null)
        {
         
        }
        private void OnLoadingStateChanged(bool isLoading)

        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }
        protected async Task ExecuteSafeAsync(Func<Task> action, string defaultErrorMessage = "Ocurrió un error inesperado en el sistema.")
        {
            LoadingService.StartLoading();
            try
            {
                await action();
            }
            catch (WebApiException apiEx)
            {
                // Manejo de errores controlados del API/Servicio
                await DialogService.ShowError(
                    message: $"Detalles: {apiEx.ErrorDetails ?? "Sin detalles."}",
                    title: $"Error de Servicio ({apiEx.StatusCode})",
                    primaryText: "Cerrar"
                );
            }
            catch (Exception ex)
            {
                await DialogService.ShowError(
                    message: ex.Message,
                    title: "Error del Sistema",
                    details: defaultErrorMessage
                );
                // Opcional: Loggear el error completo aquí
            }
            finally
            {
               LoadingService.StopLoading(); 
            }
        }
    }
}
