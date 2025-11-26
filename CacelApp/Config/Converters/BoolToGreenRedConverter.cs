using System.Globalization;
using System.Windows.Data;
using Color = System.Windows.Media.Color;

namespace CacelApp.Config.Converters;

public class BoolToGreenRedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            // Si es verdadero (en línea), retorna Verde
            if (isOnline)
            {
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Verde (#4CAF50)
            }
            // Si es falso (offline), retorna Rojo
            else
            {
                return new SolidColorBrush(Color.FromRgb(229, 57, 53)); // Rojo (#E53935)
            }
        }
        return new SolidColorBrush(Colors.Gray); // Valor por defecto
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}