using System;
using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

public class BooleanToWidthConverter : IValueConverter
{
    public double TrueWidth { get; set; } = 150;
    public double FalseWidth { get; set; } = 0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueWidth : FalseWidth;
        }
        return FalseWidth;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
