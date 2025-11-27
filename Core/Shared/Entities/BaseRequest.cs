
namespace Core.Shared.Entities;

public class BaseRequest
{
    public string action { get; set; } = ActionType.Create;
}

/// <summary>
/// Tipos de acción para peticiones a la API
/// Define las operaciones CRUD y consulta disponibles
/// </summary>
public static class  ActionType
{
    /// <summary>
    /// Crear nuevo registro (C)
    /// </summary>
    public static string Create = "C";

    /// <summary>
    /// Actualizar registro existente (U)
    /// </summary>
    public static string Update = "U";

    /// <summary>
    /// Eliminar registro (D)
    /// </summary>
    public static string Delete = "D";

    /// <summary>
    /// Buscar/listar registros (G)
    /// </summary>
    public static string Search="G";

    /// <summary>
    /// Encontrar registro específico (I)
    /// </summary>
    public static string Find="I";

    /// <summary>
    /// Seleccionar para combo box (S)
    /// </summary>
    public static string Select="S";
}


