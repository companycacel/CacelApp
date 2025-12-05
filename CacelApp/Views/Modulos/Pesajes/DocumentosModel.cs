using CacelApp.Services.Dialog;
using CacelApp.Services.Loading;
using CacelApp.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Repositories.Pesajes.Entities;
using Infrastructure.Services.Pesajes;
using System.Collections.ObjectModel;

namespace CacelApp.Views.Modulos.Pesajes;

/// <summary>
/// ViewModel para el modal de búsqueda de documentos
/// </summary>
public partial class DocumentosModel : ViewModelBase
{
    private readonly IPesajesSearchService _pesajesSearchService;

    [ObservableProperty]
    private ObservableCollection<DocumentoPes> documentos = new();

    [ObservableProperty]
    private ObservableCollection<DocumentoPes> documentosFiltrados = new();

    [ObservableProperty]
    private DocumentoPes? documentoSeleccionado;

    [ObservableProperty]
    private string? filtroBusqueda;

    public IAsyncRelayCommand SeleccionarCommand { get; }
    public IAsyncRelayCommand CancelarCommand { get; }

    public DocumentosModel(
        IDialogService dialogService,
        ILoadingService loadingService,
        IPesajesSearchService pesajesSearchService) : base(dialogService, loadingService)
    {
        _pesajesSearchService = pesajesSearchService ?? throw new ArgumentNullException(nameof(pesajesSearchService));

        SeleccionarCommand = SafeCommand(SeleccionarAsync);
        CancelarCommand = SafeCommand(CancelarAsync);
    }

    public async Task InicializarAsync()
    {
        try
        {
            LoadingService.StartLoading();

            var response = await _pesajesSearchService.GetDocumentosAsync();

            if (response?.Data != null)
            {
                Documentos.Clear();
                foreach (var doc in response.Data)
                {
                    Documentos.Add(doc);
                }

                AplicarFiltro();
            }
        }
        catch (Exception ex)
        {
            await DialogService.ShowError($"Error al cargar documentos: {ex.Message}", "Error");
        }
        finally
        {
            LoadingService.StopLoading();
        }
    }

    partial void OnFiltroBusquedaChanged(string? value)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        DocumentosFiltrados.Clear();

        var filtro = FiltroBusqueda?.ToLower() ?? "";

        var documentosFiltradosTemp = string.IsNullOrWhiteSpace(filtro)
            ? Documentos
            : Documentos.Where(d =>
                (d.mde_mov_des?.ToLower().Contains(filtro) ?? false) ||
                (d.mde_bie_des?.ToLower().Contains(filtro) ?? false));

        foreach (var doc in documentosFiltradosTemp)
        {
            DocumentosFiltrados.Add(doc);
        }
    }

    private async Task SeleccionarAsync()
    {
        if (DocumentoSeleccionado == null)
        {
            await DialogService.ShowWarning("Seleccione un documento", "Validación");
            return;
        }

        var confirmar = await DialogService.ShowConfirm(
            $"¿Desea seleccionar el documento {DocumentoSeleccionado.mde_mov_des}?",
            "Confirmar selección");

        if (!confirmar) return;

        RequestClose?.Invoke();
    }

    private async Task CancelarAsync()
    {
        DocumentoSeleccionado = null;
        RequestClose?.Invoke();
        await Task.CompletedTask;
    }
}
