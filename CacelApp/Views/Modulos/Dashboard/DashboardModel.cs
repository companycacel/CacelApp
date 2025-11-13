using CacelApp.Shared;
using CacelApp.Shared.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Dashboard;

public partial class DashboardModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<BalanzaStatus> _balanzas;

    [ObservableProperty]
    private ObservableCollection<BalanzaStatus> _camaraStatus;

    [ObservableProperty]
    private string _pesajesHoy = "1,204"; // Simulación

    [ObservableProperty]
    private string _produccionMes = "45.2 Ton"; // Simulación

    [ObservableProperty]
    private string _sistemaStatus = "Operativo"; // Simulación

    public DashboardModel()
    {
        LoadStatusData();
        SimulateWeightCapture();
    }

    private void LoadStatusData()
    {
        // Simulación de carga de datos para el Dashboard (Estado de Balanzas)
        Balanzas = new ObservableCollection<BalanzaStatus>
            {
                new BalanzaStatus { Name = "Balanza B1-A", StatusText = "En línea", IsOnline = true, IconKind = PackIconKind.ScaleBalance , CurrentWeight = 0},
                new BalanzaStatus { Name = "Balanza B2-A", StatusText = "Offline", IsOnline = false, IconKind = PackIconKind.ScaleBalance , CurrentWeight = 0}
            };

        // Simulación de carga de datos para el Dashboard (Estado de Cámaras)
        CamaraStatus = new ObservableCollection<BalanzaStatus>
            {
                new BalanzaStatus { Name = "Cámara B1-A", StatusText = "Grabando", IsOnline = true, IconKind = PackIconKind.Video },
                new BalanzaStatus { Name = "Cámara B2-A", StatusText = "Error de conexión", IsOnline = false, IconKind = PackIconKind.Video }
            };
    }
    private async void SimulateWeightCapture()
    {
        await Task.Delay(5000); // Esperar 5 segundos

        // Simular que la balanza B1-A recibe una lectura
        var balanza1 = Balanzas.FirstOrDefault(b => b.Name == "Balanza B1-A");
        if (balanza1 != null)
        {
            balanza1.CurrentWeight = 1234.50m; // Notificará el cambio vía ObservableProperty
        }

        await Task.Delay(3000);

        // Simular que la balanza B1-A recibe otra lectura, por ejemplo, 0
        if (balanza1 != null)
        {
            balanza1.CurrentWeight = 0m;
        }
    }
}

