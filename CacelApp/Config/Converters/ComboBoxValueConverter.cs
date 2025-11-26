using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

/// <summary>
/// Convierte el valor seleccionado del ComboBox (object) al tipo esperado por la propiedad destino (int, int?, string, etc).
/// </summary>
public class ComboBoxValueConverter : IValueConverter
{
    /// <summary>
    /// Convierte desde el ViewModel (int?, int, string) hacia el ComboBox (object)
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        // Si el valor es numérico, normalizarlo a int para comparación
        if (value is int || value is int? || value is long || value is long?)
        {
            if (value is int i)
                return i;
            if (value is long l)
                return (int)l;
            if (int.TryParse(value.ToString(), out int result))
                return result;
        }

        return value;
    }

    /// <summary>
    /// Convierte desde el ComboBox (object) hacia el ViewModel (int?, int, string)
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        // Convertir a int o int? según el tipo de destino
        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            if (value is int i)
                return i;
            if (value is long l)
                return (int)l;
            if (int.TryParse(value.ToString(), out int result))
                return result;

            // Si el targetType es int? y no se pudo convertir, retornar null
            if (targetType == typeof(int?))
                return null;
        }

        if (targetType == typeof(string))
        {
            return value?.ToString();
        }

        return value;
    }
}

