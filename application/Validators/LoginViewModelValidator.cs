using application.ViewModels;
using FluentValidation;

namespace application.Validators
{
    /// <summary>
    /// Validator pour LoginViewModel
    /// Valide la connexion d'un utilisateur
    /// </summary>
    public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
    {
        public LoginViewModelValidator()
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
