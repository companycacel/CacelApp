using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

public class StringIsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isEmpty = string.IsNullOrEmpty(value as string);

        // Si ConverterParameter es true, invertir el resultado
        if (parameter is true || (parameter is string s && bool.Parse(s)))
        {
            return !isEmpty;
        }

        return isEmpty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
