using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de lista de pesajes
/// Optimizado para binding en DataGrid/ListView con propiedades observables
/// Usa los nombres de los campos de la base de datos
/// </summary>
public partial class PesajesItemDto : ObservableObject
{
    [ObservableProperty] private int pes_id;
    [ObservableProperty] private string? pes_des;
    [ObservableProperty] private string? pes_mov_des;
    [ObservableProperty] private string? pes_referencia;
    [ObservableProperty] private DateTime pes_fecha;
    [ObservableProperty] private string? pes_baz_des;
    [ObservableProperty] private int pes_status;
    [ObservableProperty] private string pes_status_des;
    [ObservableProperty] private string? pes_gus_des;
    [ObservableProperty] private DateTime updated;
    [ObservableProperty] private string pes_tipo;
    [ObservableProperty] private int? pes_baz_id;
    
    // Propiedades calculadas
    [ObservableProperty] private bool canEdit;
    [ObservableProperty] private bool canDelete;
}
