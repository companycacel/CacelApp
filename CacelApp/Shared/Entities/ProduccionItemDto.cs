using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de producción (pde) en el listado
/// </summary>
public partial class ProduccionItemDto : ObservableObject
{
    [ObservableProperty] private int pde_id;
    [ObservableProperty] private int pde_pes_id;
    [ObservableProperty] private string? pde_pes_des;
    [ObservableProperty] private DateTime pde_pes_fecha;
    [ObservableProperty] private string? pde_bie_des; // Material
    [ObservableProperty] private string? pde_mde_des; // Medida
    [ObservableProperty] private string? pde_nbza; // Balanza
    [ObservableProperty] private decimal pde_pb; // Peso Bruto
    [ObservableProperty] private decimal pde_pt; // Peso Tara
    [ObservableProperty] private decimal pde_pn; // Peso Neto
    [ObservableProperty] private string? pde_gus_des; // Colaborador
    [ObservableProperty] private string? pde_obs;
    [ObservableProperty] private string? pde_path;
    [ObservableProperty] private string? pde_media;
    [ObservableProperty] private int pde_status;
    [ObservableProperty] private string pde_status_des;
    
    // Propiedades calculadas
    [ObservableProperty] private bool canEdit;
    [ObservableProperty] private bool canDelete;
    [ObservableProperty] private bool hasMedia;
}
