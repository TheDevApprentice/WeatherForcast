using domain.DTOs.WeatherForecast;
using FluentValidation;

namespace api.Validators
{
    /// <summary>
    /// Validator pour CreateWeatherForecastRequest (API)
    /// Valide les données de l'API REST
    /// </summary>
    public class CreateWeatherForecastRequestValidator : AbstractValidator<CreateWeatherForecastRequest>
    {
        public CreateWeatherForecastRequestValidator()
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
                .WithMessage("Le résumé est requis.")
                .MaximumLength(50)
                .WithMessage("Le résumé ne peut pas dépasser 50 caractères.");

            // Validation de la température
            RuleFor(x => x.TemperatureC)
                .InclusiveBetween(-100, 100)
                .WithMessage("La température doit être entre -100°C et 100°C.");
        }
    }
}
