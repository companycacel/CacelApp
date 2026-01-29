using System.Globalization;
using System.Windows.Data;
using Color = System.Windows.Media.Color;

namespace CacelApp.Config.Converters;

public class EnvironmentBadgeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string badge)
        {
            return badge switch
            {
                "PROD" => new SolidColorBrush(Color.FromRgb(25, 181, 39)), // verde
                "DEV" => new SolidColorBrush(Color.FromRgb(191, 191, 42)),  // amarillo
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