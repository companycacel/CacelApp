using Core.Shared.Entities;
using Core.Shared.Entities.Generic;
using Microsoft.AspNetCore.Http;


namespace Core.Repositories.Balanza.Entities;

/// <summary>
/// Entidad de dominio que representa un registro de pesaje en la balanza.
/// Encapsula la lógica de negocio relacionada con pesajes.
/// </summary>
public class Baz: BaseRequest
{
    public int baz_id { get; set; }
    public string? baz_des { get; set; }
    public int? baz_nro { get; set; }
    public DateTime? baz_fecha { get; set; }
    public int? baz_age_id { get; set; }
    public decimal? baz_pb { get; set; }
    public decimal? baz_pt { get; set; }
    public decimal? baz_pn { get; set; }
    public int? baz_gus_id { get; set; }
    public int? baz_col_id { get; set; } = null;
    public string baz_doc { get; set; }
    public string baz_obs { get; set; }
    public string baz_ref { get; set; }
    public int? baz_status { get; set; } = 1;
    public int? baz_t10 { get; set; } = 0;
    public string? baz_media { get; set; }
    public string? baz_path { get; set; }
    public string? baz_media1 { get; set; }
    public int? baz_t1m_id { get; set; } = 9;
    public int? baz_tra_id { get; set; }
    public string baz_veh_id { get; set; }
    public decimal baz_monto { get; set; }
    public int? baz_tipo { get; set; } = 0;
    public DateTime? created { get; set; }
    public DateTime? updated { get; set; }
    public int? baz_order { get; set; } = 0;
    public int? baz_gpe_id { get; set; }
    public object baz_age_des { get; set; }
    public string baz_gus_des { get; set; }
    public int? veh_veh_neje { get; set; }
    public List<IFormFile>? files { get; set; }


    // Propiedades de navegación (relaciones)
    public Veh? veh { get; set; }
    public Age? age { get; set; }
    public Tra? tra { get; set; }
    public Gpe? gpe { get; set; }

    /// <summary>
    /// Obtiene el tipo de operación como texto legible
    /// </summary>
    public string ObtenerTipoOperacion() => baz_tipo switch
    {
        0 => "Cliente Externo",
        1 => "Interno Despacho",
        2 => "Interno Recepción",
        _ => "Desconocido"
    };

    /// <summary>
    /// Verifica si el registro es válido para ser procesado
    /// </summary>
    public bool EsValido() =>
        !string.IsNullOrWhiteSpace(baz_veh_id) &&
        baz_pb.HasValue && baz_pb > 0 &&
        baz_pt.HasValue && baz_pt >= 0 &&
        baz_status == 1;

    /// <summary>
    /// Calcula el peso neto automáticamente si no está especificado
    /// </summary>
    public void CalcularPesoNeto()
    {
        if (baz_pb.HasValue && baz_pt.HasValue)
        {
            baz_pn = baz_pb.Value - baz_pt.Value;
        }
    }

    /// <summary>
    /// optener ruta completa de la imagen
    /// </summary>
    /// 
    public string ObtenerNombreImagen()
    {
        if (string.IsNullOrEmpty(baz_media) || string.IsNullOrEmpty(baz_media1))
            return string.Empty;

        return $"{baz_media}/{baz_media1}";
    }
}
