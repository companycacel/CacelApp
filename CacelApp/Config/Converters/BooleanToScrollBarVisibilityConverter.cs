using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

/// <summary>
/// Convierte un booleano a ScrollBarVisibility
/// true = Auto (mostrar cuando sea necesario)
/// false = Disabled (ocultar siempre)
/// </summary>
public class BooleanToScrollBarVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        }
        return ScrollBarVisibility.Disabled;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
