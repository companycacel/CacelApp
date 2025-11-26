using Core.Shared.Entities.Generic;

namespace Core.Shared.Validators;

/// <summary>
/// Validador de reglas de negocio para producción
/// Centraliza todas las validaciones del módulo de producción
/// </summary>
public static class ProduccionValidator
{
    /// <summary>
    /// Valida un registro de producción antes de crear o actualizar
    /// </summary>
    /// <param name="registro">Registro de producción a validar</param>
    /// <exception cref="ArgumentException">Se lanza cuando la validación falla</exception>
    public static void Validar(Pde registro)
    {
        if (registro == null)
            throw new ArgumentException("El registro de producción es requerido");

        // Validar ID del material
        if (registro.pde_bie_id <= 0)
            throw new ArgumentException("Debe seleccionar un material");

        // Validar balanza
        if (string.IsNullOrWhiteSpace(registro.pde_nbza))
            throw new ArgumentException("Debe especificar la balanza utilizada");

        // Validar peso bruto
        if (registro.pde_pb <= 0)
            throw new ArgumentException("El peso bruto debe ser mayor a cero");

        // Validar que la tara no sea mayor al peso bruto
        if (registro.pde_pt > registro.pde_pb)
            throw new ArgumentException("El peso tara no puede ser mayor al peso bruto");

        // Validar peso neto calculado
        var pesoNetoCalculado = registro.pde_pb - registro.pde_pt;
        if (pesoNetoCalculado <= 0)
            throw new ArgumentException("El peso neto debe ser mayor a cero");

        // Validar que el peso neto coincida con el calculado (con tolerancia de 0.01)
        if (Math.Abs(registro.pde_pn - pesoNetoCalculado) > 0.01)
            throw new ArgumentException($"El peso neto ({registro.pde_pn}) no coincide con el calculado ({pesoNetoCalculado})");

        // Validar tipo de detalle
        if (registro.pde_tipo != 1 && registro.pde_tipo != 2)
            throw new ArgumentException("El tipo de detalle debe ser 1 (Salida) o 2 (Entrada)");
    }

    /// <summary>
    /// Valida que un ID sea válido para operaciones de consulta o eliminación
    /// </summary>
    /// <param name="id">ID a validar</param>
    /// <exception cref="ArgumentException">Se lanza cuando el ID no es válido</exception>
    public static void ValidarId(int id)
    {
        if (id <= 0)
            throw new ArgumentException("El ID debe ser mayor a cero");
    }
}
