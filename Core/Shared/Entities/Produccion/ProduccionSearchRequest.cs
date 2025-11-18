using System;

namespace Core.Shared.Entities.Produccion;

/// <summary>
/// Objeto de solicitud para búsqueda de registros de producción
/// Contiene los filtros disponibles para consultar producción
/// </summary>
public class ProduccionSearchRequest
{
    /// <summary>
    /// Fecha de inicio del rango de búsqueda
    /// </summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>
    /// Fecha fin del rango de búsqueda
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// ID del material para filtrar
    /// </summary>
    public int? MaterialId { get; set; }

    /// <summary>
    /// Texto de búsqueda general
    /// </summary>
    public string? TextoBusqueda { get; set; }
}
