using Core.Shared.Entities;

namespace Core.Repositories.Profile;

public class PermisoModulo
{
    public int id { get; set; }
    public string path { get; set; }
    public string icon { get; set; }
    public string title { get; set; }
    public string type { get; set; }
    public int order { get; set; }
    public int? parent { get; set; }
    public bool hasChildren { get; set; }
}


