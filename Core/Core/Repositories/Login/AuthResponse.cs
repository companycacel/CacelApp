using Core.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Repositories.Login;

public class AuthResponse: ApiResponse<AuthData>
{
}
public class AuthData
{
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    // **IMPORTANTE:** Asumimos que el token JWT viene en este mismo objeto.
    [JsonPropertyName("token")]
    public string Token { get; set; }
}
