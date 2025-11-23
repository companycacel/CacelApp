namespace Core.Shared.Configuration;

/// <summary>
/// Configuración de FTP/HTTP para almacenamiento de imágenes
/// </summary>
public class FtpConfig
{
    public string CarpetaLocal { get; set; } = "D://FTP";
    public string ServidorUrl { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = ""; // Encriptado
}
