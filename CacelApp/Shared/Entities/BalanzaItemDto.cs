using CommunityToolkit.Mvvm.ComponentModel;


namespace CacelApp.Shared.Entities;

public partial class BalanzaItemDto : ObservableObject
{
    public string Codigo { get; set; }
    public string Placa { get; set; }
    public string Referencia { get; set; }
    public DateTime Fecha { get; set; }
    public decimal PesoBruto { get; set; }
    public decimal PesoTara { get; set; }
    public decimal PesoNeto { get; set; }
    public string Operacion { get; set; }
    public decimal Monto { get; set; }
    public string Usuario { get; set; }
    public bool EstadoOK { get; set; } // Representa el check/estado del registro
}
