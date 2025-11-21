using System;
using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

/// <summary>
/// Convierte el valor seleccionado del ComboBox (object) al tipo esperado por la propiedad destino (int, int?, string, etc).
/// </summary>
public class ComboBoxValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            if (value is int i)
                return i;
            if (int.TryParse(value.ToString(), out int result))
                return result;
            return null;
        }
        if (targetType == typeof(string))
        {
            return value.ToString();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return null;

        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            if (value is int i)
                return i;
            if (int.TryParse(value.ToString(), out int result))
                return result;
            return null;
        }
        if (targetType == typeof(string))
        {
            return value.ToString();
        }
        return value;
    }
}

