using MaterialDesignThemes.Wpf;
using System.Globalization;
using System.Windows.Data;

namespace CacelApp.Config.Converters;

public class BoolToTickCrossConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOk)
        {
            return isOk ? PackIconKind.CheckCircleOutline : PackIconKind.AlertCircleOutline;
        }
        return PackIconKind.HelpCircleOutline;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
