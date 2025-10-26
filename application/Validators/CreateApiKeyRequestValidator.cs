using FluentValidation;

namespace application.Validators
{
    /// <summary>
    /// DTO pour la création d'une clé API
    /// </summary>
    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? ExpirationDays { get; set; }
    }

    /// <summary>
    /// Validator pour CreateApiKeyRequest
    /// Valide les données avant création d'une clé API
    /// </summary>
    public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
    {
        public CreateApiKeyRequestValidator()
        {
            // Validation du nom
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Le nom de la clé API ne peut pas être vide.")
                .MaximumLength(100)
                .WithMessage("Le nom ne peut pas dépasser 100 caractères.")
                .Matches("^[a-zA-Z0-9 _-]+$")
                .WithMessage("Le nom ne peut contenir que des lettres, chiffres, espaces, tirets et underscores.");

            // Validation du nombre de jours d'expiration
            RuleFor(x => x.ExpirationDays)
                .GreaterThan(0)
                .When(x => x.ExpirationDays.HasValue)
                .WithMessage("Le nombre de jours d'expiration doit être positif.")
                .LessThanOrEqualTo(365)
                .When(x => x.ExpirationDays.HasValue)
                .WithMessage("Le nombre de jours d'expiration ne peut pas dépasser 365 jours.");
        }
    }
}
