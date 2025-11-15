
namespace Core.Shared.Entities;

public class ApiResponse<T>
{   /// <summary>
    /// status 
    /// 0=>error 
    /// 1=>ok
    /// 2=>puede ser any
    /// </summary>
    public int status { get; set; }
    public T Data { get; set; }
    public MetaBase Meta { get; set; }
}
public class MetaBase
{
    public string msg { get; set; }
}