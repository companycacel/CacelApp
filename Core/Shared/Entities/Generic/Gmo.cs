
using Core.Shared.Entities.Generic.Enums;

namespace Core.Shared.Entities.Generic;

public class Gmo
{
    public int gmo_id { get; set; }
    public string gmo_titulo { get; set; }
    public string? gmo_descripcion { get; set; }
    public int? gmo_gmo_id { get; set; }
    public string gmo_path { get; set; }
    public string gmo_icon { get; set; }
    public typeGmo gmo_type { get; set; } = typeGmo.N;
    public int gmo_order { get; set; }
    public int gmo_status { get; set; } = 1;

    public List<Gtp> gtps { get; set; } = new();
}
