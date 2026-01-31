using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace CacelApp.Config.Converters
{
    /// <summary>
    /// Permite hacer binding a una propiedad especificada por un string.
    /// Uso: MultiBinding con [0]=Objeto, [1]=PathString
    /// </summary>
    public class ReflectionBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null;

            object source = values[0];
            string path = values[1] as string;

            if (source == null || string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                return GetPropertyValue(source, path);
            }
            catch
            {
                return null;
            }
        }

        private object GetPropertyValue(object src, string propName)
        {
            if (src == null) return null;
            if (string.IsNullOrEmpty(propName)) return null;

            if (propName.Contains("."))
            {
                var split = propName.Split(new char[] { '.' }, 2);
                var firstProp = GetPropertyValueSimple(src, split[0]);
                return GetPropertyValue(firstProp, split[1]);
            }
            else
            {
                return GetPropertyValueSimple(src, propName);
            }
        }

        private object GetPropertyValueSimple(object src, string propName)
        {
            if (src == null) return null;

            // Soporte para JsonElement (System.Text.Json)
            if (src is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.Object && je.TryGetProperty(propName, out var element))
                {
                    return element.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String => element.GetString(),
                        System.Text.Json.JsonValueKind.Number => element.GetDouble(),
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Null => null,
                        _ => element
                    };
                }
                return null;
            }

            Type type = src.GetType();

            // Soportar propiedades
            PropertyInfo prop = type.GetProperty(propName);
            if (prop != null)
                return prop.GetValue(src, null);

            // Soportar campos (opcional, pero util para objetos anonimos a veces)
            FieldInfo field = type.GetField(propName);
            if (field != null)
                return field.GetValue(src);

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
