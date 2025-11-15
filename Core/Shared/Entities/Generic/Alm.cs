namespace Core.Shared.Entities.Generic;

public class Alm
{
    public int alm_id { get; set; }
    public string alm_nombre { get; set; }
    public string alm_direccion { get; set; }
    public string? alm_imagen { get; set; }
    public int alm_est_id { get; set; }
    public int alm_gdi_id { get; set; }
    public int alm_status { get; set; } = 1;
    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }

    //public est est { get; set; }
    //public List<ord> ords { get; set; } = new();
    public List<Pes> pess { get; set; } = new();
    public List<Bie> bies { get; set; } = new();
}
