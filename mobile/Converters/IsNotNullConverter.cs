using System.Globalization;

namespace mobile.Converters
{
    /// <summary>
    /// Convertit une valeur en booléen (true si non null, false si null)
    /// </summary>
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
