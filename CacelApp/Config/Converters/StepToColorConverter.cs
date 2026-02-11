using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace CacelApp.Config.Converters
{
    public class StepToColorConverter : IValueConverter
    {
        public SolidColorBrush ActiveBrush { get; set; } = new SolidColorBrush(Colors.Green); // Default: Green
        public SolidColorBrush InactiveBrush { get; set; } = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9CA3AF (Gray-400)

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentStep && parameter != null && int.TryParse(parameter.ToString(), out int targetStep))
            {
                
                if (currentStep >= targetStep)
                {
                    return ActiveBrush;
                }
            }
            return InactiveBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
