using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Implementación del validador de dispositivos por módulo
/// </summary>
public class ModuleDeviceValidator : IModuleDeviceValidator
{
    /// <summary>
    /// Valida que la sede tenga la configuración correcta para el módulo
    /// </summary>
    public DeviceValidationResult ValidateForModule(SedeConfig sede, string moduleName)
    {
        var result = new DeviceValidationResult { IsValid = true };

        if (sede == null)
        {
            result.IsValid = false;
            result.Message = "No hay sede configurada.";
            result.Errors.Add("Sede es null");
            return result;
        }

        var balanzasActivas = sede.Balanzas.Where(b => b.Activa).ToList();

        switch (moduleName.ToLower())
        {
            case "balanza":
                ValidateBalanzaModule(sede, balanzasActivas, result);
                break;

            case "pesajes":
                ValidatePesajesModule(sede, balanzasActivas, result);
                break;

            case "produccion":
                ValidateProduccionModule(sede, balanzasActivas, result);
                break;

            default:
                result.Warnings.Add($"Módulo '{moduleName}' no reconocido para validación.");
                break;
        }

        // Construir mensaje final
        if (!result.IsValid)
        {
            result.Message = $"Configuración inválida para módulo {moduleName}: {string.Join(", ", result.Errors)}";
        }
        else if (result.Warnings.Any())
        {
            result.Message = $"Configuración válida con advertencias: {string.Join(", ", result.Warnings)}";
        }
        else
        {
            result.Message = $"Configuración válida para módulo {moduleName}.";
        }

        return result;
    }

    /// <summary>
    /// Valida configuración para módulo Balanza
    /// </summary>
    private void ValidateBalanzaModule(SedeConfig sede, List<BalanzaConfig> balanzasActivas, DeviceValidationResult result)
    {
        // Módulo Balanza requiere exactamente 1 balanza
        if (balanzasActivas.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("El módulo Balanza requiere al menos 1 balanza activa.");
        }
        else if (balanzasActivas.Count > 1)
        {
            result.Warnings.Add($"El módulo Balanza solo usa 1 balanza, pero hay {balanzasActivas.Count} configuradas. Se usará la primera.");
        }

        // El módulo Balanza no requiere cámaras, pero puede tenerlas
        if (sede.Tipo != TipoSede.Balanza)
        {
            result.Warnings.Add($"La sede está configurada como tipo '{sede.Tipo}', pero se está usando en módulo Balanza.");
        }
    }

    /// <summary>
    /// Valida configuración para módulo Pesajes
    /// </summary>
    private void ValidatePesajesModule(SedeConfig sede, List<BalanzaConfig> balanzasActivas, DeviceValidationResult result)
    {
        // Módulo Pesajes requiere 1-2 balanzas
        if (balanzasActivas.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("El módulo Pesajes requiere al menos 1 balanza activa.");
        }
        else if (balanzasActivas.Count > 2)
        {
            result.Warnings.Add($"El módulo Pesajes usa máximo 2 balanzas, pero hay {balanzasActivas.Count} configuradas. Se usarán las primeras 2.");
        }

        // Verificar tipo de sede
        if (sede.Tipo != TipoSede.Pesajes)
        {
            result.Warnings.Add($"La sede está configurada como tipo '{sede.Tipo}', se recomienda tipo 'Pesajes' para este módulo.");
        }

        // Cámaras son opcionales pero recomendadas
        var camarasActivas = sede.Camaras.Where(c => c.Activa).ToList();
        if (camarasActivas.Count == 0)
        {
            result.Warnings.Add("No hay cámaras configuradas. Se recomienda configurar cámaras para el módulo Pesajes.");
        }
    }

    /// <summary>
    /// Valida configuración para módulo Producción
    /// </summary>
    private void ValidateProduccionModule(SedeConfig sede, List<BalanzaConfig> balanzasActivas, DeviceValidationResult result)
    {
        // Módulo Producción requiere 1-2 balanzas
        if (balanzasActivas.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("El módulo Producción requiere al menos 1 balanza activa.");
        }
        else if (balanzasActivas.Count > 2)
        {
            result.Warnings.Add($"El módulo Producción usa máximo 2 balanzas, pero hay {balanzasActivas.Count} configuradas. Se usarán las primeras 2.");
        }

        // Verificar tipo de sede
        if (sede.Tipo != TipoSede.Produccion)
        {
            result.Warnings.Add($"La sede está configurada como tipo '{sede.Tipo}', se recomienda tipo 'Produccion' para este módulo.");
        }

        // Cámaras son opcionales pero recomendadas
        var camarasActivas = sede.Camaras.Where(c => c.Activa).ToList();
        if (camarasActivas.Count == 0)
        {
            result.Warnings.Add("No hay cámaras configuradas. Se recomienda configurar cámaras para el módulo Producción.");
        }
    }

    /// <summary>
    /// Obtiene las balanzas activas apropiadas para el módulo
    /// </summary>
    public IEnumerable<BalanzaConfig> GetBalanzasForModule(SedeConfig sede, string moduleName)
    {
        if (sede == null) return Enumerable.Empty<BalanzaConfig>();

        var balanzasActivas = sede.Balanzas.Where(b => b.Activa).ToList();

        return moduleName.ToLower() switch
        {
            "balanza" => balanzasActivas.Take(1), // Solo la primera
            "pesajes" => balanzasActivas.Take(2), // Máximo 2
            "produccion" => balanzasActivas.Take(2), // Máximo 2
            _ => balanzasActivas
        };
    }

    /// <summary>
    /// Obtiene las cámaras activas apropiadas para el módulo
    /// </summary>
    public IEnumerable<CamaraConfig> GetCamarasForModule(SedeConfig sede, string moduleName)
    {
        if (sede == null) return Enumerable.Empty<CamaraConfig>();

        var camarasActivas = sede.Camaras.Where(c => c.Activa).ToList();

        return moduleName.ToLower() switch
        {
            "balanza" => Enumerable.Empty<CamaraConfig>(), // Balanza no usa cámaras
            "pesajes" => camarasActivas, // Todas las cámaras activas
            "produccion" => camarasActivas, // Todas las cámaras activas
            _ => camarasActivas
        };
    }
}
