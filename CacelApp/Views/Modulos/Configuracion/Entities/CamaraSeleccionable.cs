
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.Configuration;

namespace CacelApp.Views.Modulos.Configuracion.Entities;

public partial class CamaraSeleccionable : ObservableObject
{
    public CamaraConfig Camara { get; set; }

    [ObservableProperty]
    private bool _isSelected;
}
