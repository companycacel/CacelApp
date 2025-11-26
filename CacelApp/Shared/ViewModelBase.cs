using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared.Controls.DataTable;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Exceptions;
using System.Reflection;

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
        protected ViewModelBase() : this(null, null)
        {

        }
        private void OnLoadingStateChanged(bool isLoading)

        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }
        protected async Task<bool> ExecuteSafeAsync(Func<Task> action, string defaultErrorMessage = "Ocurrió un error inesperado en el sistema.")
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
                return true;
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
                return false;
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
                return false;
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
        /// <summary>
        /// Método centralizado para ejecutar la carga, mapeo y actualización de datos en el DataTable.
        /// </summary>
        protected async Task<bool> ExecuteDataLoadAsync<TEntity, TItemDto>(
            Func<Task<IEnumerable<TEntity>>> dataFetcher,
            Func<TEntity, TItemDto> dtoMapper,
            Func<TEntity, int> dataIdExtractor,
            Dictionary<int, TEntity> registrosCompletos,
            DataTableViewModel<TItemDto> tableViewModel,
            Action<List<TItemDto>>? statsUpdater = null,
            string loadingMessage = "Error al cargar registros")
            where TEntity : class
            where TItemDto : class // La restricción es solo a 'class'
        {
            // Usa ExecuteSafeAsync para manejar LoadingService y excepciones.
            return await ExecuteSafeAsync(async () =>
            {
                var data = await dataFetcher();
                var dataList = data.ToList();

                var indexProperty = typeof(TItemDto).GetProperty("Index", BindingFlags.Public | BindingFlags.Instance);
                bool canSetIndex = indexProperty != null && indexProperty.CanWrite;
                // 1. Limpiar y guardar los registros completos
                registrosCompletos.Clear();

                // 2. Mapeo a DTOs y población del diccionario
                var items = dataList.Select((reg, index) =>
                {
                    var id = dataIdExtractor(reg);

                    // Guardar la entidad completa
                    registrosCompletos[id] = reg;

                    var dto = dtoMapper(reg);

                    // ASIGNACIÓN DE ÍNDICE GENÉRICA Y LIMPIA:
                    if (canSetIndex)
                    {
                        indexProperty!.SetValue(dto, index + 1);
                    }

                    return dto;
                }).ToList();

                // 3. Cargar datos en la tabla
                tableViewModel.SetData(items);

                // 4. Actualizar estadísticas
                statsUpdater?.Invoke(items);

            }, loadingMessage);
        }
    }
}
