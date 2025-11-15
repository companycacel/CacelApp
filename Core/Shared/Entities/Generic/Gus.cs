


using Core.Repositories.Balanza.Entities;

namespace Core.Shared.Entities.Generic;

public class  Gus
{
    public int gus_id { get; set; }
    public int gus_gpe_id { get; set; }
    public string? gus_imagen { get; set; }
    public int gus_gro_id { get; set; }
    public string gus_user { get; set; }
    public string gus_password { get; set; }
    public string gus_month { get; set; } = "2025-09%";
    public string? gus_token { get; set; }
    public string? gus_codpais { get; set; } = "+51";
    public string? gus_telefono { get; set; }
    public int gus_gcl_id { get; set; }
    public int? gus_gar_id { get; set; }
    public int gus_status { get; set; } = 1;
    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }

    public Gpe gpe { get; set; }
    public Gro gro { get; set; }
    public List<Pes> pess { get; set; } = new();
    public List<Baz> bazs { get; set; } = new();

}
