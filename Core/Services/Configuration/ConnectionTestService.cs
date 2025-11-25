using Core.Shared.Configuration;
using System.Diagnostics;
using System.IO.Ports;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para probar conexiones de balanzas, DVR, API y FTP
/// Basado en implementación de CacelTracking
/// </summary>
public class ConnectionTestService : IConnectionTestService
{
    public async Task<ConnectionTestResult> TestBalanzaAsync(BalanzaConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConnectionTestResult();
        
        try
        {
            // Basado en CacelTracking: Main.cs líneas 169-193
            using var serialPort = new SerialPort(config.Puerto, config.BaudRate, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };
            
            serialPort.Open();
            
            // Intentar leer datos
            await Task.Delay(500); // Esperar datos
            var data = serialPort.ReadExisting();
            
            serialPort.Close();
            
            result.Success = true;
            result.Message = $"✅ Balanza {config.Nombre} conectada correctamente";
            result.AdditionalInfo["Puerto"] = config.Puerto;
            result.AdditionalInfo["BaudRate"] = config.BaudRate;
            result.AdditionalInfo["DatosRecibidos"] = !string.IsNullOrEmpty(data);
            
            if (!string.IsNullOrEmpty(data))
            {
                var preview = data.Length > 20 ? data.Substring(0, 20) + "..." : data;
                result.AdditionalInfo["UltimaLectura"] = preview;
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.Success = false;
            result.Message = $"❌ Puerto {config.Puerto} en uso por otra aplicación";
        }
        catch (IOException ex)
        {
            result.Success = false;
            result.Message = $"❌ Error de I/O en puerto {config.Puerto}: {ex.Message}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"❌ Error al conectar balanza: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<ConnectionTestResult> TestDvrAsync(DvrConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConnectionTestResult();
        
        try
        {
            // TODO: Implementar cuando se integre Dahua NetSDK
            // Por ahora, solo validar que la IP sea alcanzable
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            try
            {
                var puerto = (config.Puerto > 0) ? $":{config.Puerto}" : "";
                var response = await client.GetAsync($"http://{config.Ip}{puerto}");
                result.Success = true;
                result.Message = $"✅ DVR Dahua alcanzable en {config.Ip}:{config.Puerto}";
            }
            catch
            {
                // Si HTTP falla, intentar ping básico
                result.Success = false;
                result.Message = $"⚠️ No se pudo conectar al DVR. Verificar IP y puerto.";
            }
            
            result.AdditionalInfo["Ip"] = config.Ip;
            result.AdditionalInfo["Puerto"] = config.Puerto;
            result.AdditionalInfo["Modelo"] = "Dahua";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"❌ Error al conectar DVR: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<ConnectionTestResult> TestWebApiAsync(string url)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConnectionTestResult();
        
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync($"{url}/health");
            
            if (!response.IsSuccessStatusCode)
            {
                response = await client.GetAsync(url);
            }
            
            result.Success = response.IsSuccessStatusCode;
            result.Message = result.Success 
                ? "✅ API conectada correctamente" 
                : $"⚠️ API respondió con código {response.StatusCode}";
            result.AdditionalInfo["StatusCode"] = (int)response.StatusCode;
            result.AdditionalInfo["Url"] = url;
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.Message = $"❌ No se pudo conectar a la API: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            result.Success = false;
            result.Message = "❌ Timeout: La API no respondió en 5 segundos";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"❌ Error al conectar API: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<ConnectionTestResult> TestFtpAsync(FtpConfig config)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConnectionTestResult();
        
        try
        {
            // Verificar carpeta local
            if (!Directory.Exists(config.CarpetaLocal))
            {
                try
                {
                    Directory.CreateDirectory(config.CarpetaLocal);
                    result.AdditionalInfo["CarpetaCreada"] = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"❌ No se pudo crear carpeta local: {ex.Message}";
                    return result;
                }
            }
            

            result.AdditionalInfo["CarpetaLocal"] = config.CarpetaLocal;
            result.AdditionalInfo["ServidorUrl"] = config.ServidorUrl;
     
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"❌ Error al conectar FTP: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
            result.ResponseTime = stopwatch.Elapsed;
        }
        
        return result;
    }
}
