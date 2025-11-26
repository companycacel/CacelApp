namespace Core.Shared.Entities.Generic;

public class Gro
{
    public int gro_id { get; set; }
    public string gro_nombre { get; set; }
    public string gro_descripcion { get; set; }
    public int gro_status { get; set; } = 1;

    public List<Gtp> gtps { get; set; } = new();
    public List<Gus> guss { get; set; } = new();
}
