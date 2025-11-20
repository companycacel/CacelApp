using System;

namespace CacelApp.Views.Modulos.Balanza.Entities;

/// <summary>
/// DTO para presentación de un registro de balanza completo
/// Mapea datos de la entidad de dominio a la capa de presentación
/// </summary>
public class BalanzaRegistroDto
{
    public int Id { get; set; }
    public string? Placa { get; set; }
    public string? Descripcion { get; set; }
    public int? Numero { get; set; }
    public DateTime? Fecha { get; set; }
    public string? NombreAgencia { get; set; }
    public decimal? PesoBruto { get; set; }
    public decimal? PesoTara { get; set; }
    public decimal? PesoNeto { get; set; }
    public string? NombreUsuarioGrupo { get; set; }
    public string? Documento { get; set; }
    public string? Observaciones { get; set; }
    public string? Referencia { get; set; }
    public int Estado { get; set; }
    public string? EstadoDescripcion => Estado == 1 ? "Activo" : "Inactivo";
    public string? MediaPath { get; set; }
    public string? ArchivoPDF { get; set; }
    public string? MediaPath1 { get; set; }
    public int? TrabajoId { get; set; }
    public string? VehiculoId { get; set; }
    public decimal Monto { get; set; }
    public int? Tipo { get; set; } // 0: Cliente Externo, 1: Interno Despacho, 2: Interno Recepción
    public string? TipoDescripcion => Tipo switch
    {
        0 => "Cliente Externo",
        1 => "Interno Despacho",
        2 => "Interno Recepción",
        _ => "Desconocido"
    };
    public DateTime? FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public string? NombreGrupo { get; set; }
    public int? VehiculoNejesId { get; set; }
    public string? ImagenPath { get; set; }
}
