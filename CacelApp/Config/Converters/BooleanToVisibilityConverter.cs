using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CacelApp.Config.Converters
{
    /// <summary>
    /// Convertidor para cambiar la visibilidad basada en un valor booleano
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle boolean values
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            
            // Handle object null checks (for binding to objects like SedeConfig)
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

}