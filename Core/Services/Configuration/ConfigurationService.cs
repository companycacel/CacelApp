using Core.Shared.Configuration;
using Core.Shared.Helpers;
using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para gestionar la configuración de la aplicación
/// Persistencia en AppData\Local\CacelApp\config.json
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _appDataPath;
    private readonly string _configFilePath;
    private readonly string _backupFilePath;
    private readonly string _exportsPath;
    private readonly string _appSettingsPath;
    private AppConfiguration? _currentConfig;
    private AppSettings? _appSettings;
    
    public AppConfiguration? CurrentConfiguration => _currentConfig;
    
    // Evento para notificar cambios en la configuración
    public event EventHandler? ConfigurationChanged;
    
    public ConfigurationService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CacelApp"
        );
        _configFilePath = Path.Combine(_appDataPath, "config.json");
        _backupFilePath = Path.Combine(_appDataPath, "config.backup.json");
        _exportsPath = Path.Combine(_appDataPath, "exports");
        _appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        
        // Crear directorios si no existen
        Directory.CreateDirectory(_appDataPath);
        Directory.CreateDirectory(_exportsPath);
    }
    
    public async Task<AppConfiguration> LoadAsync()
    {
        if (_currentConfig != null) return _currentConfig;
        
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _currentConfig = JsonSerializer.Deserialize<AppConfiguration>(json);
                
                if (_currentConfig != null)
                {
                    // Desencriptar passwords
                    DesencriptarPasswords(_currentConfig);
                    return _currentConfig;
                }
            }
            
            // Primera vez - crear configuración por defecto
            _currentConfig = CreateDefaultConfiguration();
            await SaveAsync(_currentConfig);
            return _currentConfig;
        }
        catch (Exception)
        {
            // Si falla, intentar restaurar backup
            if (File.Exists(_backupFilePath))
            {
                return await RestoreBackupAsync();
            }
            
            // Si todo falla, crear configuración por defecto
            _currentConfig = CreateDefaultConfiguration();
            return _currentConfig;
        }
    }
    
    public async Task SaveAsync(AppConfiguration config)
    {
        // Crear backup antes de guardar
        await CreateBackupAsync();
        
        config.UltimaActualizacion = DateTime.Now;
        
        // Encriptar passwords antes de guardar
        var configToSave = CloneAndEncryptPasswords(config);
        
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        var json = JsonSerializer.Serialize(configToSave, options);
        await File.WriteAllTextAsync(_configFilePath, json);
        
        _currentConfig = config;
        
        // Notificar que la configuración ha cambiado
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public async Task CreateBackupAsync()
    {
        if (File.Exists(_configFilePath))
        {
            File.Copy(_configFilePath, _backupFilePath, true);
        }
        await Task.CompletedTask;
    }
    
    public async Task<AppConfiguration> RestoreBackupAsync()
    {
        if (!File.Exists(_backupFilePath))
            throw new FileNotFoundException("No se encontró archivo de backup");
        
        var json = await File.ReadAllTextAsync(_backupFilePath);
        var config = JsonSerializer.Deserialize<AppConfiguration>(json);
        
        if (config == null)
            throw new InvalidOperationException("Backup inválido");
        
        DesencriptarPasswords(config);
        _currentConfig = config;
        return config;
    }
    
    public async Task<string> ExportAsync(string? filePath = null)
    {
        var config = await LoadAsync();
        
        if (string.IsNullOrEmpty(filePath))
        {
            var fileName = $"config_{DateTime.Now:yyyy-MM-dd_HHmmss}.json";
            filePath = Path.Combine(_exportsPath, fileName);
        }
        
        // Encriptar passwords antes de exportar
        var configToExport = CloneAndEncryptPasswords(config);
        
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        var json = JsonSerializer.Serialize(configToExport, options);
        await File.WriteAllTextAsync(filePath, json);
        
        return filePath;
    }
    
    public async Task<AppConfiguration> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Archivo no encontrado: {filePath}");
        
        var json = await File.ReadAllTextAsync(filePath);
        var config = JsonSerializer.Deserialize<AppConfiguration>(json);
        
        if (config == null)
            throw new InvalidOperationException("Configuración inválida");
        
        // Validar configuración
        foreach (var sede in config.Sedes)
        {
            if (!sede.EsValida())
                throw new InvalidOperationException($"Sede '{sede.Nombre}' tiene configuración inválida");
        }
        
        // Desencriptar passwords
        DesencriptarPasswords(config);
        
        await SaveAsync(config);
        return config;
    }
    
    public async Task<SedeConfig?> GetSedeActivaAsync()
    {
        if (_currentConfig == null)
            await LoadAsync();
        
        return _currentConfig?.GetSedeActiva();
    }
    
    public async Task SetSedeActivaAsync(int sedeId)
    {
        if (_currentConfig == null)
            await LoadAsync();
        
        if (_currentConfig != null)
        {
            _currentConfig.SedeActivaId = sedeId;
            await SaveAsync(_currentConfig);
        }
    }
    
    private AppConfiguration CreateDefaultConfiguration()
    {
        return new AppConfiguration
        {
            EquipoNombre = Environment.MachineName,
            Version = "1.0.0",
            Global = new GlobalConfig
            {
                Environment = "Development", // Por defecto Development
                Ftp = new FtpConfig
                {
                    CarpetaLocal = "D://FTP",
                    ServidorUrl = "http://38.253.154.34:8086"
                }
            },
            Sedes = new ObservableCollection<SedeConfig>
            {
                new SedeConfig
                {
                    Id = 1,
                    Nombre = "Sede Principal",
                    Codigo = "SEDE_A",
                    Tipo = TipoSede.Pesajes,
                    Balanzas = new ObservableCollection<BalanzaConfig>
                    {
                        new BalanzaConfig { Id = 1, Nombre = "B1-A", Grupo = "A", Puerto = "COM1" },
                        new BalanzaConfig { Id = 2, Nombre = "B2-A", Grupo = "A", Puerto = "COM2" }
                    },
                    Dvr = new DvrConfig
                    {
                        Ip = "192.168.1.129",
                        Puerto = null,
                        Usuario = ""
                    },
                    Camaras = new ObservableCollection<CamaraConfig>
                    {
                        new CamaraConfig { Id = 1, Canal = 1, Nombre = "Cámara 1", Ubicacion = "Entrada" },
                        new CamaraConfig { Id = 2, Canal = 2, Nombre = "Cámara 2", Ubicacion = "Salida" }
                    }
                }
            },
            SedeActivaId = 1
        };
    }
    
    private void DesencriptarPasswords(AppConfiguration config)
    {
        // FTP
        if (!string.IsNullOrEmpty(config.Global.Ftp.Password))
        {
            config.Global.Ftp.Password = PasswordEncryption.Decrypt(config.Global.Ftp.Password);
        }
        
        // DVR de cada sede
        foreach (var sede in config.Sedes)
        {
            if (!string.IsNullOrEmpty(sede.Dvr.Password))
            {
                sede.Dvr.Password = PasswordEncryption.Decrypt(sede.Dvr.Password);
            }
        }
    }
    
    private AppConfiguration CloneAndEncryptPasswords(AppConfiguration config)
    {
        // Serializar y deserializar para clonar
        var json = JsonSerializer.Serialize(config);
        var clone = JsonSerializer.Deserialize<AppConfiguration>(json)!;
        
        // Encriptar passwords en el clon
        if (!string.IsNullOrEmpty(clone.Global.Ftp.Password))
        {
            clone.Global.Ftp.Password = PasswordEncryption.Encrypt(clone.Global.Ftp.Password);
        }
        
        foreach (var sede in clone.Sedes)
        {
            if (!string.IsNullOrEmpty(sede.Dvr.Password))
            {
                sede.Dvr.Password = PasswordEncryption.Encrypt(sede.Dvr.Password);
            }
        }
        
        return clone;
    }
    
    public AppSettings LoadAppSettings()
    {
        if (_appSettings != null) return _appSettings;
        
        try
        {
            if (File.Exists(_appSettingsPath))
            {
                var json = File.ReadAllText(_appSettingsPath);
                _appSettings = JsonSerializer.Deserialize<AppSettings>(json);
                
                if (_appSettings != null)
                    return _appSettings;
            }
            return _appSettings;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("No se pudo cargar appsettings.json");
        }  
    }
    
    public string GetCurrentApiUrl()
    {
        var appSettings = LoadAppSettings();
        
        // Intentar leer el entorno desde el archivo de configuración sin cargar todo
        string environment = "Development"; // Por defecto
        
        try
        {
            if (_currentConfig != null)
            {
                environment = _currentConfig.Global?.Environment ?? "Development";
            }
            else if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(json);
                environment = config?.Global?.Environment ?? "Development";
            }
        }
        catch
        {
            // Si falla, usar Development por defecto
        }
        
        return environment.ToLower() switch
        {
            "production" => appSettings.ApiUrls.Production,
            _ => appSettings.ApiUrls.Development
        };
    }
}
