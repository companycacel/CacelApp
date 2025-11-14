using System;
using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

public class BoolToHelperTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Si value es false (inválido), mostrar el texto de ayuda del parámetro
        // Si value es true (válido), retornar cadena vacía
        if (value is bool isValid)
        {
            System.Diagnostics.Debug.WriteLine($"BoolToHelperTextConverter: isValid={isValid}, parameter={parameter}");
            return !isValid ? (parameter?.ToString() ?? "") : "";
        }
        System.Diagnostics.Debug.WriteLine($"BoolToHelperTextConverter: value is not bool, value={value}");
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
