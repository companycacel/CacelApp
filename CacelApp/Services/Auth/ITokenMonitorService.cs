
namespace CacelApp.Services.Auth;

public interface ITokenMonitorService
{
    void StartMonitoring(DateTime expirationTime);
    void StopMonitoring();
}
