using Core.Shared.Entities;
using Core.Shared.Validators;
using System.Text.Json.Serialization;

namespace Core.Repositories.Profile;

public class UserProfileResponse : ApiResponse<UserProfileData>
{
}
public class UserProfileData
{
    public int gus_id { get; set; }
    public int gus_gpe_id { get; set; }
    public string? gus_imagen { get; set; }
    public int gus_gro_id { get; set; }
    public string? gus_user { get; set; }
    public string? gus_password { get; set; }
    public string? gus_month { get; set; }
    public string? gus_token { get; set; }
    public string? gus_codpais { get; set; }
    public string? gus_telefono { get; set; }
    public int gus_gcl_id { get; set; }
    public int gus_gar_id { get; set; }
    public int gus_status { get; set; }
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public UserPersonData? gpe { get; set; }
    public int gus_alm_id { get; set; }
    public string? gus_env { get; set; }
}

public class UserPersonData
{
    public int gpe_id { get; set; }
    public string? gpe_identificacion { get; set; }
    public string? gpe_nombre { get; set; }
    public string? gpe_apellidos { get; set; }
    public string? gpe_direccion { get; set; }
    public string? gpe_codpais { get; set; }
    public string? gpe_telefono { get; set; }
    public string? gpe_ecivil { get; set; }
    public string? gpe_nacionalidad { get; set; }
    public string? gpe_fechan { get; set; }
    public string? gpe_genero { get; set; }
    public int gpe_gt2_id { get; set; }
    public int gpe_valid { get; set; }
    public string? gpe_gdi_id { get; set; }
    public string? gpe_email { get; set; }
    public int gpe_status { get; set; }

    [JsonConverter(typeof(ToStringNullableConverter))]
    public string? col { get; set; }
}
