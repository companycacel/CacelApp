using System;

namespace CacelApp.Shared.Entities;

/// <summary>
/// DTO para búsqueda y filtrado de registros de balanza
/// Contiene los criterios de filtro que se envían desde la UI
/// </summary>
public class BalanzaBusquedaDto
{
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? VehiculoId { get; set; }
    public string? AgenciaDescripcion { get; set; }
    public int? Estado { get; set; }
    public int? Tipo { get; set; }
    public string? Referencia { get; set; }
}
