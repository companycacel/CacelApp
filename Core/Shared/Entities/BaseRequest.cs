
namespace Core.Shared.Entities;

public class BaseRequest
{
    public string action { get; set; } = ActionType.Create;
}

public static class ActionType
{
    public const string Create = "C";
    public const string Update = "U";
    public const string Delete = "D";
    /// <summary>
    /// listados
    /// </summary>
    public const string Search = "G";
    public const string Find = "I";

    /// <summary>
    /// combo box
    /// </summary>
    public const string Select = "S";
}
