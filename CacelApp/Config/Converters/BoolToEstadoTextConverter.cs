using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

public class BoolToEstadoTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOk)
        {
            return isOk ? "Estado OK" : "Estado con Error";
        }
        return "Estado desconocido";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
