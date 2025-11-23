using System.Text.Json.Serialization;

namespace Core.Shared.Configuration;

/// <summary>
/// Configuración de una cámara en el DVR
/// </summary>
public class CamaraConfig
{
    public int Id { get; set; }
    public int Canal { get; set; } // Canal en el DVR (1-based)
    public string Nombre { get; set; } = ""; // Ej: "Cámara 1", "Cámara 2"
    public string Ubicacion { get; set; } = ""; // Ej: "Entrada", "Salida"
    public bool Activa { get; set; } = true;
    
    // Estado (runtime - no se guarda en JSON)
    [JsonIgnore]
    public bool Conectada { get; set; }
}
