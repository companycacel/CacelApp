using System;
using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

public class BoolToHelperTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return !isValid ? (parameter?.ToString() ?? "") : "";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
