

using Core.Repositories.Balanza.Entities;

namespace Core.Shared.Entities.Generic;

public class Col
{
    public int col_id { get; set; }
    public int col_gpe_id { get; set; }
    public int? col_est_id { get; set; }
    public string? col_img { get; set; }
    public string? col_cuenta { get; set; }
    public bool? col_viaja { get; set; }
    public int col_status { get; set; } = 1;
    public string? col_obs { get; set; }
    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }

    public List<Pes> pess { get; set; } = new();
    public Gpe gpe { get; set; }
    public List<Baz> baz { get; set; } = new();
}
