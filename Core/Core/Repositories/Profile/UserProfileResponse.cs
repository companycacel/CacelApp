using Core.Shared.Entities;
using System.Text.Json.Serialization;

namespace Core.Repositories.Profile;

public class UserProfileResponse : ApiResponse<UserProfileData>
{
}
public class UserProfileData
{
    [JsonPropertyName("gus_id")]
    public int GusId { get; set; }

    [JsonPropertyName("gus_gpe_id")]
    public int GusGpeId { get; set; }

    [JsonPropertyName("gus_imagen")]
    public string? GusImagen { get; set; }

    [JsonPropertyName("gus_gro_id")]
    public int GusGroId { get; set; }

    [JsonPropertyName("gus_user")]
    public string? GusUser { get; set; }

    [JsonPropertyName("gus_password")]
    public string? GusPassword { get; set; }

    [JsonPropertyName("gus_month")]
    public string? GusMonth { get; set; }

    [JsonPropertyName("gus_token")]
    public string? GusToken { get; set; }

    [JsonPropertyName("gus_codpais")]
    public string? GusCodePais { get; set; }

    [JsonPropertyName("gus_telefono")]
    public string? GusTelefono { get; set; }

    [JsonPropertyName("gus_gcl_id")]
    public int GusGclId { get; set; }

    [JsonPropertyName("gus_gar_id")]
    public int GusGarId { get; set; }

    [JsonPropertyName("gus_status")]
    public int GusStatus { get; set; }

    [JsonPropertyName("created")]
    public DateTime Created { get; set; }

    [JsonPropertyName("updated")]
    public DateTime Updated { get; set; }

    [JsonPropertyName("gpe")]
    public UserPersonData? Gpe { get; set; }

    [JsonPropertyName("gus_alm_id")]
    public int GusAlmId { get; set; }

    [JsonPropertyName("gus_env")]
    public string? GusEnv { get; set; }
}

public class UserPersonData
{
    [JsonPropertyName("gpe_id")]
    public int GpeId { get; set; }

    [JsonPropertyName("gpe_identificacion")]
    public string? GpeIdentificacion { get; set; }

    [JsonPropertyName("gpe_nombre")]
    public string? GpeNombre { get; set; }

    [JsonPropertyName("gpe_apellidos")]
    public string? GpeApellidos { get; set; }

    [JsonPropertyName("gpe_direccion")]
    public string? GpeDireccion { get; set; }

    [JsonPropertyName("gpe_codpais")]
    public string? GpeCodePais { get; set; }

    [JsonPropertyName("gpe_telefono")]
    public string? GpeTelefono { get; set; }

    [JsonPropertyName("gpe_ecivil")]
    public string? GpeECivil { get; set; }

    [JsonPropertyName("gpe_nacionalidad")]
    public string? GpeNacionalidad { get; set; }

    [JsonPropertyName("gpe_fechan")]
    public string? GpeFechan { get; set; }

    [JsonPropertyName("gpe_genero")]
    public string? GpeGenero { get; set; }

    [JsonPropertyName("gpe_gt2_id")]
    public int GpeGt2Id { get; set; }

    [JsonPropertyName("gpe_valid")]
    public int GpeValid { get; set; }

    [JsonPropertyName("gpe_gdi_id")]
    public string? GpeGdiId { get; set; }

    [JsonPropertyName("gpe_email")]
    public string? GpeEmail { get; set; }

    [JsonPropertyName("gpe_status")]
    public int GpeStatus { get; set; }

    [JsonPropertyName("col")]
    public string? Col { get; set; }
}
