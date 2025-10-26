using application.ViewModels;
using FluentValidation;

namespace application.Validators
{
    /// <summary>
    /// Validator pour WeatherForecastViewModel
    /// Valide les données de présentation avant création/modification
    /// </summary>
    public class WeatherForecastViewModelValidator : AbstractValidator<WeatherForecastViewModel>
    {
        public WeatherForecastViewModelValidator()
        {
            // Validation de la date (utilise Must pour éviter les problèmes client-side)
            RuleFor(x => x.Date)
                .Must(date => date.Date >= DateTime.UtcNow.Date.AddYears(-1))
                .WithMessage("La date ne peut pas être antérieure à 1 an")
                .Must(date => date.Date <= DateTime.UtcNow.Date.AddYears(1))
                .WithMessage("La date ne peut pas être supérieure à 1 an dans le futur");

            // Validation du résumé
            RuleFor(x => x.Summary)
                .NotEmpty()
                .WithMessage("Veuillez sélectionner un résumé météo valide.")
                .Must(summary => !IsInvalidSummary(summary))
                .WithMessage("Veuillez sélectionner un résumé météo valide.");

            // Validation de la température
            RuleFor(x => x.TemperatureC)
                .InclusiveBetween(-100, 100)
                .WithMessage("La température doit être entre -100°C et 100°C.");
        }

        /// <summary>
        /// Vérifie si le résumé est invalide (placeholder ou vide)
        /// </summary>
        private static bool IsInvalidSummary(string? summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return true;

            var invalidSummaries = new[]
            {
                "-- Sélectionnez --",
                "--Sélectionnez--",
                "Sélectionnez"
            };

            return invalidSummaries.Contains(summary.Trim(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
