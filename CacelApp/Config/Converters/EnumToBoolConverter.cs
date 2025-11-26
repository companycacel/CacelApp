using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace CacelApp.Config.Converters;

/// <summary>
/// Converter para convertir un enum a boolean para RadioButtons
/// Permite enlazar RadioButtons a propiedades enum
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();

        return enumValue == targetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null || !(value is bool isChecked) || !isChecked)
            return Binding.DoNothing;

        try
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        catch
        {
            return Binding.DoNothing;
        }
    }
}
