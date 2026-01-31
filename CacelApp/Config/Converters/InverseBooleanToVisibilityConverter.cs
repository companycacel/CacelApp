using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CacelApp.Config.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle boolean values (inverse)
            if (value is bool boolean)
                return boolean ? Visibility.Collapsed : Visibility.Visible;

            // Handle object null checks (inverse - show when null)
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
