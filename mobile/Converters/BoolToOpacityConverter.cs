using System.Globalization;

namespace mobile.Converters
{
    /// <summary>
    /// Convertit un bool en opacité (true = 1.0, false = 0.5)
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1.0 : 0.5;
            }
            return 1.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
