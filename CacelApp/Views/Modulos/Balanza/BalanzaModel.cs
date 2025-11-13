using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace CacelApp.Views.Modulos.Balanza;

public partial class BalanzaModel : ObservableObject
{
    [ObservableProperty] private DateTime? _fechaInicio = DateTime.Now.AddDays(-7);
    [ObservableProperty] private DateTime? _fechaFinal = DateTime.Now;
    [ObservableProperty] private string _filtroPlaca;
    [ObservableProperty] private string _filtroCliente;

    // Propiedades de Datos
    [ObservableProperty]
    private ObservableCollection<BalanzaItemDto> _registros;

    [ObservableProperty]
    private BalanzaItemDto _selectedRegistro;

    // Comandos
    public IAsyncRelayCommand BuscarCommand { get; }
    public IAsyncRelayCommand AgregarCommand { get; }
    public IRelayCommand EditarCommand { get; }
    public IRelayCommand EliminarCommand { get; }

    public BalanzaModel(/* IBalanzaService balanzaService */)
    {
        BuscarCommand = new AsyncRelayCommand(BuscarRegistrosAsync);
        AgregarCommand = new AsyncRelayCommand(AgregarRegistroAsync);
        EditarCommand = new RelayCommand(EditarRegistro);
        EliminarCommand = new RelayCommand(EliminarRegistro);

        // Carga inicial
        BuscarRegistrosAsync();
    }

    private async Task BuscarRegistrosAsync()
    {
        // Lógica para llamar al servicio (ej: await _balanzaService.GetRegistrosAsync(filtros);)
        // Por ahora, simulación:
        await Task.Delay(500);

        // Simulación de los datos del mockup
        Registros = new ObservableCollection<BalanzaItemDto>(GenerateMockData());
    }

    private async Task AgregarRegistroAsync()
    {
        // Lógica para abrir el diálogo o ventana de nuevo registro
        System.Windows.MessageBox.Show("Abriendo formulario para nuevo registro.");
        await Task.CompletedTask;
    }

    private void EditarRegistro()
    {
        if (SelectedRegistro != null)
        {
            System.Windows.MessageBox.Show($"Editar registro: {SelectedRegistro.Codigo}");
        }
    }

    private void EliminarRegistro()
    {
        if (SelectedRegistro != null)
        {
            // Lógica de confirmación y eliminación.
            System.Windows.MessageBox.Show($"Eliminando registro: {SelectedRegistro.Codigo}");
        }
    }

    // Función auxiliar para generar datos de simulación
    private List<BalanzaItemDto> GenerateMockData()
    {
        // ... (Implementación de simulación de datos del mockup)
        return Enumerable.Range(1, 15).Select(i => new BalanzaItemDto
        {
            Codigo = $"TX00-00{140 + i}",
            Placa = $"D3R{10 + i}10",
            Referencia = (i % 3 == 0) ? "MOLINO" : "CLIENTE",
            Fecha = DateTime.Now.AddDays(-i),
            PesoBruto = 5000 + (i * 100),
            PesoTara = 500,
            PesoNeto = 4500 + (i * 100),
            Operacion = (i % 2 == 0) ? "Cliente Externo" : "Interno Despacho",
            Monto = 10.0m + i,
            Usuario = "Herly",
            EstadoOK = (i % 5 != 0)
        }).ToList();
    }
}
