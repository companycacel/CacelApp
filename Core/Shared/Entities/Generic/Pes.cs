

using Core.Repositories.Balanza.Entities;

namespace Core.Shared.Entities.Generic;

public class Pes : BaseRequest
{


    public int pes_id { get; set; }

    public int pes_nro { get; set; }
    public string? pes_des { get; set; }
    public int pes_alm_id { get; set; }
    public int pes_gus_id { get; set; }
    public string pes_gus_des { get; set; }
    public int? pes_pes_id { get; set; }
    public string? pes_referencia { get; set; }
    public string? pes_mov_des { get; set; }
    public int? pes_mov_id { get; set; }
    public DateTime pes_fecha { get; set; } = DateTime.Now;
    public int? pes_col_id { get; set; }
    public string pes_tipo { get; set; }
    public string? pes_obs { get; set; }
    public int pes_status { get; set; } = 1;
    public int? pes_baz_id { get; set; }
    public string? pes_baz_des { get; set; }

    public object pes_cond_id;
    // Propiedad calculada para descripción de estado
    public string pes_status_des => pes_status switch
    {
        1 => "Procesado",
        2 => "Registrando",
        _ => "Desconocido"
    };

    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }

    public Gus gus { get; set; }
    public Alm alm { get; set; }
    public Baz? baz { get; set; }
    public Col? col { get; set; }
    public Pes? _ref { get; set; }
    public List<Pes> pess { get; set; } = new();
    public List<Pde> pdes { get; set; } = new();
}

