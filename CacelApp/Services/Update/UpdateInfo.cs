namespace CacelApp.Services.Update;

/// <summary>
/// Información sobre una actualización disponible
/// </summary>
public class UpdateInfo
{
    /// <summary>
    /// Versión de la actualización disponible
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño de la actualización en bytes
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Notas de la versión
    /// </summary>
    public string? ReleaseNotes { get; set; }

    /// <summary>
    /// URL de descarga
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Fecha de publicación
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Tamaño formateado para mostrar al usuario
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (SizeBytes < 1024)
                return $"{SizeBytes} B";
            if (SizeBytes < 1024 * 1024)
                return $"{SizeBytes / 1024.0:F2} KB";
            if (SizeBytes < 1024 * 1024 * 1024)
                return $"{SizeBytes / (1024.0 * 1024.0):F2} MB";
            return $"{SizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}
