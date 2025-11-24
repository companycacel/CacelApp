using System.Collections.ObjectModel;

namespace Core.Shared.Configuration;

/// <summary>
/// Configuración de una sede (multi-sede)
/// Máximo 2 balanzas por sede
/// </summary>
public class SedeConfig
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public TipoSede Tipo { get; set; } = TipoSede.Pesajes;
    
    // Balanzas (máximo 2) ⚠️
    public ObservableCollection<BalanzaConfig> Balanzas { get; set; } = new();
    
    // DVR (Dahua)
    public DvrConfig Dvr { get; set; } = new();
    
    // Cámaras (n cámaras)
    public ObservableCollection<CamaraConfig> Camaras { get; set; } = new();
    
    /// <summary>
    /// Valida que la configuración de la sede sea correcta
    /// </summary>
    public bool EsValida()
    {
        // IMPORTANTE: Máximo 2 balanzas por sede
        if (Balanzas.Count > 2)
            return false;
            
        return !string.IsNullOrEmpty(Nombre) && 
               !string.IsNullOrEmpty(Codigo);
    }
}
