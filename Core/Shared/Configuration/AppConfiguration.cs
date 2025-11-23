namespace Core.Shared.Configuration;

/// <summary>
/// Configuración completa de la aplicación
/// Se guarda en AppData\Local\CacelApp\config.json
/// </summary>
public class AppConfiguration
{
    // Información del equipo
    public string EquipoNombre { get; set; } = Environment.MachineName;
    public string Version { get; set; } = "1.0.0";
    public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    
    // Configuración Global (única para toda la app)
    public GlobalConfig Global { get; set; } = new();
    
    // Sedes (multi-sede)
    public List<SedeConfig> Sedes { get; set; } = new();
    
    // Sede actualmente seleccionada
    public int SedeActivaId { get; set; }
    
    /// <summary>
    /// Obtiene la sede activa actual
    /// </summary>
    public SedeConfig? GetSedeActiva()
    {
        return Sedes.FirstOrDefault(s => s.Id == SedeActivaId);
    }
}
