using api.DTOs;
using FluentValidation;

namespace api.Validators
{
    /// <summary>
    /// Validator pour UpdateWeatherForecastRequest (API)
    /// Valide la mise à jour d'une prévision météo
    /// </summary>
    public class UpdateWeatherForecastRequestValidator : AbstractValidator<UpdateWeatherForecastRequest>
    {
        public UpdateWeatherForecastRequestValidator()
        {
            // Validation de la date
            RuleFor(x => x.Date)
                .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-1))
                .WithMessage("La date ne peut pas être antérieure à 1 an")
                .LessThanOrEqualTo(DateTime.UtcNow.AddYears(1))
                .WithMessage("La date ne peut pas être supérieure à 1 an dans le futur");

            // Validation du résumé
            RuleFor(x => x.Summary)
                .NotEmpty()
                .WithMessage("Le résumé est requis.")
                .MaximumLength(100)
                .WithMessage("Le résumé ne peut pas dépasser 100 caractères.");

            // Validation de la température
            RuleFor(x => x.TemperatureC)
                .InclusiveBetween(-100, 100)
                .WithMessage("La température doit être entre -100°C et 100°C.");
        }
    }
}
