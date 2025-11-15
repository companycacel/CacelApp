namespace Core.Shared.Validators;

/// <summary>
/// Clase para validaciones comunes en toda la aplicación
/// Centraliza lógica de validación para evitar duplicación
/// </summary>
public static class ValidationHelper
{
    #region Mensajes de Error Centralizados

    public const string ErrorIdInvalido = "El ID debe ser mayor a 0";
    public const string ErrorRangoFechasInvalido = "La fecha de inicio no puede ser mayor a la fecha de fin";
    public const string ErrorFechaRequerida = "Las fechas de inicio y fin son requeridas";
    public const string ErrorTextoVacio = "El campo no puede estar vacío";
    public const string ErrorTextoNulo = "El campo no puede ser nulo";
    public const string ErrorObjetoNulo = "El objeto no puede ser nulo";

    #endregion

    #region Validaciones de ID

    /// <summary>
    /// Valida que un ID sea mayor a cero
    /// </summary>
    /// <param name="id">ID a validar</param>
    /// <param name="parametro">Nombre del parámetro para el mensaje de error</param>
    /// <exception cref="ArgumentException">Si el ID no es válido</exception>
    public static void ValidarId(int id, string parametro = "id")
    {
        if (id <= 0)
            throw new ArgumentException(ErrorIdInvalido, parametro);
    }

    /// <summary>
    /// Valida que un ID sea mayor a cero y devuelve el resultado
    /// </summary>
    /// <param name="id">ID a validar</param>
    /// <returns>True si es válido, false si no</returns>
    public static bool EsIdValido(int id) => id > 0;

    #endregion

    #region Validaciones de Fechas

    /// <summary>
    /// Valida que un rango de fechas sea válido (inicio <= fin)
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio</param>
    /// <param name="fechaFin">Fecha de fin</param>
    /// <exception cref="ArgumentException">Si el rango no es válido</exception>
    public static void ValidarRangoFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaInicio > fechaFin)
            throw new ArgumentException(ErrorRangoFechasInvalido);
    }

    /// <summary>
    /// Valida que un rango de fechas opcionales sea válido
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio opcional</param>
    /// <param name="fechaFin">Fecha de fin opcional</param>
    /// <exception cref="ArgumentException">Si ambas fechas están presentes y el rango no es válido</exception>
    public static void ValidarRangoFechasOpcional(DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (fechaInicio.HasValue && fechaFin.HasValue && fechaInicio.Value > fechaFin.Value)
            throw new ArgumentException(ErrorRangoFechasInvalido);
    }

    /// <summary>
    /// Valida que ambas fechas estén presentes
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio</param>
    /// <param name="fechaFin">Fecha de fin</param>
    /// <exception cref="ArgumentException">Si alguna fecha es nula</exception>
    public static void ValidarFechasRequeridas(DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (!fechaInicio.HasValue || !fechaFin.HasValue)
            throw new ArgumentException(ErrorFechaRequerida);
    }

    /// <summary>
    /// Valida que un rango de fechas sea válido, devuelve el resultado
    /// </summary>
    /// <param name="fechaInicio">Fecha de inicio</param>
    /// <param name="fechaFin">Fecha de fin</param>
    /// <returns>True si es válido, false si no</returns>
    public static bool EsRangoFechasValido(DateTime fechaInicio, DateTime fechaFin) => 
        fechaInicio <= fechaFin;

    #endregion

    #region Validaciones de Strings

    /// <summary>
    /// Valida que una cadena no sea nula o vacía
    /// </summary>
    /// <param name="valor">Cadena a validar</param>
    /// <param name="parametro">Nombre del parámetro para el mensaje de error</param>
    /// <exception cref="ArgumentException">Si la cadena es nula o vacía</exception>
    public static void ValidarTextoNoVacio(string? valor, string parametro = "valor")
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException(ErrorTextoVacio, parametro);
    }

    /// <summary>
    /// Valida que una cadena no sea nula
    /// </summary>
    /// <param name="valor">Cadena a validar</param>
    /// <param name="parametro">Nombre del parámetro para el mensaje de error</param>
    /// <exception cref="ArgumentNullException">Si la cadena es nula</exception>
    public static void ValidarTextoNoNulo(string? valor, string parametro = "valor")
    {
        if (valor is null)
            throw new ArgumentNullException(parametro, ErrorTextoNulo);
    }

    /// <summary>
    /// Valida que una cadena no sea nula o vacía, devuelve el resultado
    /// </summary>
    /// <param name="valor">Cadena a validar</param>
    /// <returns>True si es válido, false si no</returns>
    public static bool EsTextoValido(string? valor) => !string.IsNullOrWhiteSpace(valor);

    #endregion

    #region Validaciones de Objetos

    /// <summary>
    /// Valida que un objeto no sea nulo
    /// </summary>
    /// <typeparam name="T">Tipo del objeto</typeparam>
    /// <param name="objeto">Objeto a validar</param>
    /// <param name="parametro">Nombre del parámetro para el mensaje de error</param>
    /// <exception cref="ArgumentNullException">Si el objeto es nulo</exception>
    public static void ValidarObjetoNoNulo<T>(T? objeto, string parametro = "objeto") where T : class
    {
        if (objeto is null)
            throw new ArgumentNullException(parametro, ErrorObjetoNulo);
    }

    /// <summary>
    /// Valida que un objeto no sea nulo, devuelve el resultado
    /// </summary>
    /// <typeparam name="T">Tipo del objeto</typeparam>
    /// <param name="objeto">Objeto a validar</param>
    /// <returns>True si es válido, false si no</returns>
    public static bool EsObjetoValido<T>(T? objeto) where T : class => objeto is not null;

    #endregion
}
