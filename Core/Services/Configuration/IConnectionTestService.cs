using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para probar conexiones de balanzas, DVR, API y FTP
/// </summary>
public interface IConnectionTestService
{
    Task<ConnectionTestResult> TestWebApiAsync(string url);
    Task<ConnectionTestResult> TestFtpAsync(FtpConfig config);
    Task<ConnectionTestResult> TestDvrAsync(DvrConfig config);
    Task<ConnectionTestResult> TestBalanzaAsync(BalanzaConfig config);
}
