using Core.Shared.Configuration;

namespace Core.Services.Configuration;

/// <summary>
/// Servicio para validar que los dispositivos configurados sean apropiados para cada módulo
/// </summary>
public interface IModuleDeviceValidator
{
    /// <summary>
    /// Valida que la sede tenga la configuración de dispositivos correcta para el módulo especificado
    /// </summary>
    /// <param name="sede">Configuración de la sede a validar</param>
    /// <param name="moduleName">Nombre del módulo (Balanza, Pesajes, Produccion)</param>
    DeviceValidationResult ValidateForModule(SedeConfig sede, string moduleName);
    
    /// <summary>
    /// Obtiene las balanzas activas válidas para un módulo específico
    /// </summary>
    IEnumerable<BalanzaConfig> GetBalanzasForModule(SedeConfig sede, string moduleName);
    
    /// <summary>
    /// Obtiene las cámaras activas válidas para un módulo específico
    /// </summary>
    IEnumerable<CamaraConfig> GetCamarasForModule(SedeConfig sede, string moduleName);
}
