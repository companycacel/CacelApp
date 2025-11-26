using System.Text.Json.Serialization;

namespace Core.Shared.Configuration;

/// <summary>
/// Configuración de DVR Dahua para cámaras
/// </summary>
public class DvrConfig
{
    public string Ip { get; set; } = "";
    public int? Puerto { get; set; }
    public string Usuario { get; set; } = "admin";
    public string Password { get; set; } = ""; // Encriptado
    public string Modelo { get; set; } = "Dahua";

    // Estado de conexión (runtime - no se guarda en JSON)
    [JsonIgnore]
    public bool Conectado { get; set; }

    [JsonIgnore]
    public DateTime? UltimaConexion { get; set; }
}
