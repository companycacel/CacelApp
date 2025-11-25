using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CacelApp.Config.Converters;

public class EnvironmentBadgeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string badge)
        {
            return badge switch
            {
                "PROD" => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Rojo
                "DEV" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),  // Azul
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}