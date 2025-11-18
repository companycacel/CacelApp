using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para presentación de un elemento de detalle de producción (pde)
/// Optimizado para binding en DataGrid con propiedades observables
/// </summary>
public partial class ProduccionDetalleItemDto : ObservableObject
{
    [ObservableProperty] private int pde_id;
    [ObservableProperty] private int pde_pes_id;
    [ObservableProperty] private int? pde_mde_id;
    [ObservableProperty] private string? pde_mde_des;
    [ObservableProperty] private int pde_bie_id;
    [ObservableProperty] private string? pde_bie_des;
    [ObservableProperty] private string? pde_nbza; // Nombre de balanza
    [ObservableProperty] private decimal pde_pb; // Peso bruto
    [ObservableProperty] private decimal pde_pt; // Peso tara
    [ObservableProperty] private decimal pde_pn; // Peso neto
    [ObservableProperty] private string? pde_obs;
    [ObservableProperty] private string? pde_gus_des;
    [ObservableProperty] private DateTime created;
    [ObservableProperty] private DateTime updated;
    [ObservableProperty] private string? pde_path; // Ruta de imágenes
    [ObservableProperty] private string? pde_media; // Nombres de archivos de imágenes
    [ObservableProperty] private int? pde_t6m_id;
    [ObservableProperty] private string? pde_bie_cod;
    
    // Propiedades calculadas para UI
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isNew;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private bool canDelete = true;
    [ObservableProperty] private bool isPesoBrutoReadOnly; // Se bloquea cuando es balanza B1-A, B2-A, B3-B, B4-B
    
    // Lista de fotos capturadas (no se persiste, solo para UI)
    public List<(string nombre, byte[] contenido)>? FotosCapturas { get; set; }
    
    /// <summary>
    /// Determina si tiene imágenes capturadas
    /// </summary>
    public bool HasImages => !string.IsNullOrEmpty(Pde_media);
    
    /// <summary>
    /// Obtiene los nombres de archivos de imágenes como lista
    /// </summary>
    public List<string> GetImageNames()
    {
        if (string.IsNullOrEmpty(Pde_media))
            return new List<string>();
        
        return new List<string>(Pde_media.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }
    
    partial void OnPde_nbzaChanged(string? value)
    {
        // Bloquear peso bruto si es balanza principal
        IsPesoBrutoReadOnly = value == "B1-A" || value == "B2-A" || value == "B3-B" || value == "B4-B";
        
        // Resetear tara si no es balanza principal
        if (!IsPesoBrutoReadOnly)
        {
            Pde_pt = 0;
        }
    }
    
    partial void OnPde_pbChanged(decimal value)
    {
        // Recalcular peso neto
        Pde_pn = value - Pde_pt;
    }
    
    partial void OnPde_ptChanged(decimal value)
    {
        // Recalcular peso neto
        Pde_pn = Pde_pb - value;
    }
}
