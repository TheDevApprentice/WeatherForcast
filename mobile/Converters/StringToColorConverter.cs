using System.Globalization;

namespace mobile.Converters
{
    /// <summary>
    /// Convertit une chaîne (email) en couleur pour l'avatar
    /// </summary>
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string str || string.IsNullOrEmpty(str))
            {
                return Color.FromArgb("#667eea"); // Couleur par défaut
            }

            // Générer une couleur basée sur le hash de la chaîne
            var hash = str.GetHashCode();
            var colors = new[]
            {
                Color.FromArgb("#667eea"), // Violet
                Color.FromArgb("#764ba2"), // Violet foncé
                Color.FromArgb("#f093fb"), // Rose
                Color.FromArgb("#4facfe"), // Bleu
                Color.FromArgb("#43e97b"), // Vert
                Color.FromArgb("#fa709a"), // Rose-rouge
                Color.FromArgb("#feca57"), // Jaune
                Color.FromArgb("#ff6348"), // Rouge-orange
            };

            var index = Math.Abs(hash) % colors.Length;
            return colors[index];
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
