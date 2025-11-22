using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacelApp.Views.Modulos.Balanza.Entities;


/// <summary>
/// ViewModel para items de vehículos en la selección
/// </summary>
public partial class VehiculoItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int id;  // veh_neje (número de ejes)

    [ObservableProperty]
    private string nombre = string.Empty;

    [ObservableProperty]
    private decimal precio;  // veh_ref

    [ObservableProperty]
    private string capacidad = string.Empty;  // veh_year

    [ObservableProperty]
    private bool estaSeleccionado;

    [ObservableProperty]
    private string imagenUrl = string.Empty;
}

