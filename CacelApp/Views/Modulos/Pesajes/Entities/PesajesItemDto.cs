using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.Entities.Generic;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de lista de pesajes
/// Hereda directamente de Pes para acceso directo a propiedades sin wrapper
/// </summary>
[ObservableObject]
public partial class PesajesItemDto : Pes
{
    // Índice para numeración en la tabla
    [ObservableProperty]
    private int? index;

    // Propiedades calculadas para permisos
    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool canDelete;

    public string shortUser => pes_gus_des?.Substring(0, Math.Min(2, pes_gus_des.Length)).ToUpper() ?? "";
}
