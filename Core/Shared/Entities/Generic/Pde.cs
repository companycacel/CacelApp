using Microsoft.AspNetCore.Http;

namespace Core.Shared.Entities.Generic;

public class Pde:BaseRequest
{
    public int pde_id { get; set; }
    public string? pde_nbza { get; set; } 
    public int pde_bie_id { get; set; }
    public string pde_bie_cod { get; set; }
    public int? pde_t6m_id { get; set; } = 58;
    public int? pde_pde_id { get; set; }
    public int pde_tipo { get; set; } = 1;
    public float pde_pb { get; set; }
    public string? pde_media { get; set; }
    public string? pde_path { get; set; }
    public float pde_pt { get; set; }
    public float pde_pn { get; set; }
    public string? pde_obs { get; set; }
    public int pde_pes_id { get; set; }
    public string pde_gus_des { get; set; }

    public int? pde_mde_id { get; set; }
    public string? pde_mde_des { get; set; }

    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }
    public List<IFormFile>? files { get; set; }
    //public bie bie { get; set; }
    //public pes pes { get; set; }
    //public t6m t6m { get; set; }
    //public pde? _ref { get; set; }
    //public List<pde> pdes { get; set; } = new();
}