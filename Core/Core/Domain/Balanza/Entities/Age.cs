namespace Core.Domain.Balanza.Entities;

/// <summary>
/// Entidad que representa una agencia
/// </summary>
public class Age
{
    public int? age_id { get; set; }
    public string age_des { get; set; }
    public string age_nro { get; set; }
    public int? age_tipo { get; set; }
    public string age_email { get; set; }
    public bool age_isgem { get; set; }
    public int? age_est_id { get; set; }
    public int? age_gdi_id { get; set; }
    public int? age_gt2_id { get; set; }
    public int? age_gt4_id { get; set; }
    public int? age_ref_id { get; set; }
    public int? age_status { get; set; }
    public string age_est_des { get; set; }
    public string age_gdi_des { get; set; }
    public string age_gt2_des { get; set; }
    public string age_gt4_des { get; set; }
    public string? age_telefono { get; set; }
    public string age_direccion { get; set; }
    public string age_referencia { get; set; }
    public string age_coordenadas { get; set; }
}
