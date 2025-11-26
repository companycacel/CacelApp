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
    /// Obtiene el número máximo de balanzas permitidas según el tipo de sede
    /// </summary>
    public int GetMaxBalanzas()
    {
        return Tipo switch
        {
            TipoSede.Balanza => 1,
            TipoSede.Pesajes => 2,
            TipoSede.Produccion => 2,
            _ => 0
        };
    }

    /// <summary>
    /// Indica si este tipo de sede requiere configuración de cámaras
    /// </summary>
    public bool RequiereCamaras()
    {
        return Tipo == TipoSede.Pesajes || Tipo == TipoSede.Produccion;
    }

    /// <summary>
    /// Obtiene un mensaje de validación específico si la configuración no es válida
    /// </summary>
    public string GetValidationMessage()
    {
        if (string.IsNullOrEmpty(Nombre))
            return "El nombre de la sede es requerido.";

        if (string.IsNullOrEmpty(Codigo))
            return "El código de la sede es requerido.";

        int maxBalanzas = GetMaxBalanzas();
        if (Balanzas.Count > maxBalanzas)
            return $"El tipo de sede '{Tipo}' permite máximo {maxBalanzas} balanza(s). Actualmente tiene {Balanzas.Count}.";

        if (Balanzas.Count == 0)
            return "La sede debe tener al menos una balanza configurada.";

        return string.Empty;
    }

    /// <summary>
    /// Valida que la configuración de la sede sea correcta
    /// </summary>
    public bool EsValida()
    {
        // Validar según el tipo de sede
        int maxBalanzas = GetMaxBalanzas();
        if (Balanzas.Count > maxBalanzas || Balanzas.Count == 0)
            return false;

        return !string.IsNullOrEmpty(Nombre) &&
               !string.IsNullOrEmpty(Codigo);
    }
}
