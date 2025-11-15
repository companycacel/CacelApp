namespace Core.Shared.Entities.Generic;

public class T6m
{
    public int? t6m_id { get; set; } = null;
    public string t6m_codigo { get; set; }
    public string t6m_sunat { get; set; }
    public string t6m_descripcion { get; set; }
    public int t6m_status { get; set; } = 1;

    //public List<act> acts { get; set; } = new();
    //public List<ade> ades { get; set; } = new();
    public List<Bie> bies { get; set; } = new();
    public List<Pde> pdes { get; set; } = new();
}
