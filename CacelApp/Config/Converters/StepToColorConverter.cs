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
                // Si el parámetro tiene formato "step|color", se podría personalizar el color activo por paso
                // Por ahora, asumimos que se pasa el parámetro "2" y si currentStep == 2, usamos el color activo general o uno específico del binding si se pudiera.
                
                // Mejor enfoque para este caso específico: 
                // El converter se usará así: Background="{Binding CurrentStep, Converter={StaticResource StepToColorConverter}, ConverterParameter=1}"
                
                if (currentStep == targetStep)
                {
                    // Retornamos el color activo ("Pintado")
                    // Para soportar diferentes colores por paso (Verde, Azul, Verde, Naranja),
                    // definiremos el color activo como propiedad del converter instance en XAML
                    // o simplemente devolvemos un color "Highlight" genérico y dejamos que el XAML decida el color específico vía Style triggers?
                    // El usuario dijo "el 1 q se pinte... los demas plomo".
                    // Vamos a devolver ActiveBrush si coincide.
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
