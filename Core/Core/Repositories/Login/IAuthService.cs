
namespace Core.Repositories.Login;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(AuthRequest request);
    Task<AuthResponse> RefreshTokenAsync();
    HttpClient GetAuthenticatedClient(); 
    Task LogoutAsync();
}
