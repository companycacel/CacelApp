namespace Core.Domain.Balanza.Entities;

/// <summary>
/// Entidad que representa un vehículo en el sistema
/// </summary>
public class Veh
{
    public string veh_id { get; set; }
    public object veh_obs { get; set; }
    public int? veh_ref { get; set; }
    public int? veh_neje { get; set; }
    public string veh_tipo { get; set; }
    public object veh_year { get; set; }
}
