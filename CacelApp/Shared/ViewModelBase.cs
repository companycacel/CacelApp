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
            // Protegemos contra servicios no registrados (p.ej. instancias creadas sin DI)
            try
            {
                LoadingService?.StartLoading();
            }
            catch
            {
                // Si StartLoading lanza, no queremos que eso impida el flujo de manejo de excepciones.
            }

            try
            {
                await action();
            }
            catch (WebApiException apiEx)
            {
                // Manejo de errores controlados del API/Servicio
                if (DialogService != null)
                {
                    await DialogService.ShowError(
                        message: $" {apiEx.ErrorDetails ?? "Sin detalles."}",
                        title: $"Error de Servicio ({apiEx.StatusCode})",
                        primaryText: "Cerrar"
                    );
                }
                else
                {
                    System.Windows.MessageBox.Show(apiEx.ErrorDetails ?? "Sin detalles.", $"Error de Servicio ({apiEx.StatusCode})");
                }
            }
            catch (Exception ex)
            {
                if (DialogService != null)
                {
                    await DialogService.ShowError(
                        message: ex.Message,
                        title: "Error del Sistema",
                        details: defaultErrorMessage
                    );
                }
                else
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error del Sistema");
                }
                // Opcional: Loggear el error completo aquí
            }
            finally
            {
                try
                {
                    LoadingService?.StopLoading();
                }
                catch
                {
                    // Ignorar errores al detener el loading
                }
            }
        }
    }
}
