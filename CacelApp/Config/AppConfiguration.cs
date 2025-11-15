namespace CacelApp.Config;

/// <summary>
/// Configuración centralizada de la aplicación
/// Evita hardcodear valores en múltiples lugares
/// </summary>
public static class AppConfiguration
{
    /// <summary>
    /// Configuración de API
    /// </summary>
    public static class Api
    {
        /// <summary>
        /// URL base de la API
        /// TODO: Mover a appsettings.json o variables de entorno
        /// </summary>
        public const string BaseUrl = "http://38.253.154.34:3001";

        /// <summary>
        /// Timeout para las peticiones HTTP (en segundos)
        /// </summary>
        public const int TimeoutSeconds = 30;

        /// <summary>
        /// Número máximo de reintentos para peticiones fallidas
        /// </summary>
        public const int MaxRetries = 3;
    }

    /// <summary>
    /// Configuración de la interfaz de usuario
    /// </summary>
    public static class UI
    {
        /// <summary>
        /// Número de registros por página por defecto
        /// </summary>
        public const int DefaultPageSize = 20;

        /// <summary>
        /// Tamaño de los iconos en botones de acción
        /// </summary>
        public const int ActionButtonIconSize = 18;

        /// <summary>
        /// Ancho de los botones de acción
        /// </summary>
        public const int ActionButtonWidth = 30;

        /// <summary>
        /// Alto de los botones de acción
        /// </summary>
        public const int ActionButtonHeight = 30;
    }

    /// <summary>
    /// Configuración de validaciones de negocio
    /// </summary>
    public static class Business
    {
        /// <summary>
        /// Rango máximo de días permitido para consultas de reportes
        /// </summary>
        public const int MaxDaysRangeForReports = 365;

        /// <summary>
        /// Días hacia atrás para búsqueda por defecto
        /// </summary>
        public const int DefaultSearchDaysBack = 30;
    }

    /// <summary>
    /// Configuración de logging
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Nivel de log por defecto
        /// </summary>
        public const string DefaultLogLevel = "Information";

        /// <summary>
        /// Habilitar logging de peticiones HTTP
        /// </summary>
        public const bool LogHttpRequests = true;
    }
}
