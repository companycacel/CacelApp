using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de lista de balanza
/// Optimizado para binding en DataGrid/ListView con propiedades observables
/// </summary>
public partial class BalanzaItemDto : ObservableObject
{
    [ObservableProperty] private int? index;
    [ObservableProperty] private int id; // ID del registro para operaciones
    [ObservableProperty] private string? codigo;
    [ObservableProperty] private string? placa;
    [ObservableProperty] private string? referencia;
    [ObservableProperty] private DateTime fecha;
    [ObservableProperty] private decimal pesoBruto;
    [ObservableProperty] private decimal pesoTara;
    [ObservableProperty] private decimal pesoNeto;
    [ObservableProperty] private string? operacion;
    [ObservableProperty] private decimal monto;
    [ObservableProperty] private string? usuario;
    [ObservableProperty] private bool estadoOK;
    [ObservableProperty] private string? nombreAgencia;
    [ObservableProperty] private int? estado;
    [ObservableProperty] private string? imagenPath;
    
    // Campos necesarios para cargar imágenes (separados de imagenPath)
    [ObservableProperty] private string? bazPath;
    
    // Campos adicionales para edición
    [ObservableProperty] private int? tipoPago;  // baz_t1m_id
    [ObservableProperty] private int? vehiculoNejeId;  // veh_veh_neje
    [ObservableProperty] private int? tipoOperacion;  // baz_tipo
}
