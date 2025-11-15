namespace Core.Domain.Balanza.Entities;

/// <summary>
/// Entidad que representa un grupo de usuarios
/// </summary>
public class Gpe
{
    public int? gpe_id { get; set; }
    public string gpe_email { get; set; }
    public string gpe_ecivil { get; set; }
    public string gpe_fechan { get; set; }
    public int? gpe_gdi_id { get; set; }
    public string gpe_genero { get; set; }
    public int? gpe_gt2_id { get; set; }
    public string gpe_nombre { get; set; }
    public int? gpe_status { get; set; }
    public string gpe_celular { get; set; }
    public string gpe_codpais { get; set; }
    public string gpe_telefono { get; set; }
    public string gpe_apellidos { get; set; }
    public string gpe_direccion { get; set; }
    public string gpe_nacionalidad { get; set; }
    public string gpe_identificacion { get; set; }
}
