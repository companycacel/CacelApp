using Core.Exceptions;
using Core.Repositories.Login;
using Core.Shared.Entities;
using System.Net;
using System.Net.Http.Json;

public class AuthService : IAuthService
{
    private readonly HttpClient _baseHttpClient;
    private readonly CookieContainer _cookieContainer = new CookieContainer();
    private const string AccessTokenCookieName = "token";

  
    private string _jwtToken;
    private DateTime? _tokenExpiration;
    private string _refreshToken; 

    public AuthService(HttpClient httpClient)
    {
        _baseHttpClient = httpClient;
    }

    private HttpClient GetCookieHttpClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true
        };

        return new HttpClient(handler)
        {
            BaseAddress = _baseHttpClient.BaseAddress
        };
    }

    public HttpClient GetAuthenticatedClient()
    {
        return GetCookieHttpClient();
    }


    public void SetTokenData(DateTime expiresAt, string token)
    {
        this._jwtToken = token;
        this._tokenExpiration = expiresAt;
    }
    public string GetCurrentToken() => this._jwtToken;
    public DateTime? GetTokenExpiration() => this._tokenExpiration;
    public async Task LogoutAsync()
    {

        var client = GetCookieHttpClient();
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("logout", content: null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            response = null;
        }
        this._jwtToken = null;
        this._tokenExpiration = null;
        this._refreshToken = null;

    }

    public async Task<AuthResponse> LoginAsync(AuthRequest request)
    {
        var client = GetCookieHttpClient();

        var response = await client.PostAsJsonAsync("login", new
        {
            gus_user = request.username,
            gus_password = request.password
        });

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

            throw new WebApiException(
                message: errorJson?.message?? "Fallo la conexión o la API rechazó la solicitud.", 
                statusCode: response.StatusCode, 
                errorType: errorJson.error
            );
           
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (tokenResponse?.Meta.msg != "OK")
        {
            throw new WebApiException(tokenResponse?.Meta?.msg ?? "Credenciales inválidas.", HttpStatusCode.Unauthorized);
        }

        string tokenFromCookie = ExtractTokenValueFromCookies(client.BaseAddress, AccessTokenCookieName);

        if (string.IsNullOrEmpty(tokenFromCookie))
        {
            tokenFromCookie = tokenResponse.Data.Token;

            if (string.IsNullOrEmpty(tokenFromCookie))
            {
                throw new WebApiException("El token JWT no se encontró ni en la cookie ni en el cuerpo JSON.", HttpStatusCode.Unauthorized);
            }
        }

        SetTokenData(tokenResponse.Data.ExpiresAt, tokenFromCookie);

        return tokenResponse;
    }

    public async Task<AuthResponse> RefreshTokenAsync()
    {
        var client = GetCookieHttpClient();
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), "login");

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new WebApiException("No se pudo refrescar el token.", response.StatusCode);
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

        if (tokenResponse?.status != 1)
        {
            throw new WebApiException(tokenResponse?.Meta?.msg ?? "Fallo en el servidor al refrescar token.", HttpStatusCode.Unauthorized);
        }

        string newTokenFromCookie = ExtractTokenValueFromCookies(client.BaseAddress, AccessTokenCookieName);

        if (string.IsNullOrEmpty(newTokenFromCookie))
        {
            newTokenFromCookie = tokenResponse.Data.Token;
            if (string.IsNullOrEmpty(newTokenFromCookie))
            {
                throw new WebApiException("El nuevo token JWT no se encontró después del refresco.", HttpStatusCode.Unauthorized);
            }
        }

        SetTokenData(tokenResponse.Data.ExpiresAt, newTokenFromCookie);

        return tokenResponse;
    }

    private string ExtractTokenValueFromCookies(Uri baseUri, string cookieName)
    {
        var cookies = _cookieContainer.GetCookies(baseUri);
        var tokenCookie = cookies[cookieName];

        if (tokenCookie == null)
        {
            return null;
        }
        return tokenCookie.Value;
    }
}