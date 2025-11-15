namespace Core.Shared.Entities.Generic;

public class Gtp
{
    public int gtp_id { get; set; }
    public int gtp_r { get; set; }
    public int gtp_w { get; set; }
    public int gtp_u { get; set; }
    public int gtp_d { get; set; }
    public int gtp_gro_id { get; set; }
    public int gtp_gmo_id { get; set; }
    public int gtp_est_id { get; set; } = 1;
    public DateTime created { get; set; } = DateTime.Now;
    public DateTime updated { get; set; }

    public Gro gro { get; set; }
    public Gmo gmo { get; set; }
}
