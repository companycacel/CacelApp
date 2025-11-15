namespace Core.Shared.Entities.Generic;

public class Bie
{
    public int bie_id { get; set; }
    public string bie_codigo { get; set; }
    public string bie_nombre { get; set; }
    public int bie_t6m_id { get; set; }
    public int? bie_tipo { get; set; }
    public int? bie_bie_id { get; set; }
    public int bie_alm_id { get; set; } = 1;
    public float? bie_p { get; set; }
    public bool? bie_igv { get; set; }
    public string? bie_img { get; set; }
    public float bie_porc { get; set; } = 90.0000f;
    public int bie_status { get; set; } = 1;
    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }

    public T6m t6m { get; set; }
    public Alm alm { get; set; }

    public List<Pde> pdes { get; set; } = new();

}
