using System.Text.Json.Serialization;

namespace Core.Shared.Configuration;

/// <summary>
/// Configuración de una balanza conectada por puerto serial
/// </summary>
public class BalanzaConfig
{
    public int Id { get; set; }
    public string Nombre { get; set; } = ""; // Ej: "B1-A", "B2-A"
    public string Grupo { get; set; } = ""; // "A", "B", "Otros"
    public string Puerto { get; set; } = ""; // COM1, COM2, etc.
    public int BaudRate { get; set; } = 9600;
    public string Modelo { get; set; } = "";
    public bool Activa { get; set; } = true;
    
    // Estado de conexión (runtime - no se guarda en JSON)
    [JsonIgnore]
    public bool Conectada { get; set; }
    
    [JsonIgnore]
    public DateTime? UltimaLectura { get; set; }
    
    [JsonIgnore]
    public string? UltimoPeso { get; set; }
}
