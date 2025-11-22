using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace CacelApp.Shared.Controls.DataTable
{
    /// <summary>
    /// Convierte un valor de PackIconKind a Visibility: Visible si hay icono definido, Collapsed si es null o None.
    /// </summary>
    public class PackIconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PackIconKind kind)
            {
                // Si es None o el valor por defecto, ocultar
                if (kind == PackIconKind.None || kind.ToString() == "None")
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
            // Si es null o no es PackIconKind, ocultar
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
