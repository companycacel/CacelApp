using System;
using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace CacelApp.Config.Converters;

/// <summary>
/// Convertidor para RadioButtons que compara un valor con un parámetro
/// Útil para binding de RadioButtons con valores string
/// </summary>
public class RadioButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            return parameter.ToString();
        }

        return Binding.DoNothing;
    }
}
