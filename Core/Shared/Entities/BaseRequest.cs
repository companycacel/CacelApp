
namespace Core.Shared.Entities;

public class BaseRequest
{
    public string action { get; set; } = ActionTypeExtensions.ToApiValue(ActionType.Create);
}

/// <summary>
/// Tipos de acción para peticiones a la API
/// Define las operaciones CRUD y consulta disponibles
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Crear nuevo registro (C)
    /// </summary>
    Create,

    /// <summary>
    /// Actualizar registro existente (U)
    /// </summary>
    Update,

    /// <summary>
    /// Eliminar registro (D)
    /// </summary>
    Delete,

    /// <summary>
    /// Buscar/listar registros (G)
    /// </summary>
    Search,

    /// <summary>
    /// Encontrar registro específico (I)
    /// </summary>
    Find,

    /// <summary>
    /// Seleccionar para combo box (S)
    /// </summary>
    Select
}

/// <summary>
/// Extensiones para convertir ActionType a valores de API
/// </summary>
public static class ActionTypeExtensions
{
    /// <summary>
    /// Convierte el enum ActionType al valor de API esperado
    /// </summary>
    public static string ToApiValue(this ActionType actionType) => actionType switch
    {
        ActionType.Create => "C",
        ActionType.Update => "U",
        ActionType.Delete => "D",
        ActionType.Search => "G",
        ActionType.Find => "I",
        ActionType.Select => "S",
        _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, "Tipo de acción no válido")
    };

    /// <summary>
    /// Convierte un valor de API al enum ActionType
    /// </summary>
    public static ActionType FromApiValue(string apiValue) => apiValue switch
    {
        "C" => ActionType.Create,
        "U" => ActionType.Update,
        "D" => ActionType.Delete,
        "G" => ActionType.Search,
        "I" => ActionType.Find,
        "S" => ActionType.Select,
        _ => throw new ArgumentOutOfRangeException(nameof(apiValue), apiValue, "Valor de API no válido")
    };
}
