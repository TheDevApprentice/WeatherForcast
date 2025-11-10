using domain.DTOs.Auth;
using FluentValidation;

namespace api.Validators
{
    /// <summary>
    /// Validator pour LoginRequest (API)
    /// Valide la connexion d'un utilisateur mobile
    /// </summary>
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            // Validation de l'email
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("L'email est requis")
                .EmailAddress()
                .WithMessage("Email invalide");

            // Validation du mot de passe
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Le mot de passe est requis");
        }
    }
}
