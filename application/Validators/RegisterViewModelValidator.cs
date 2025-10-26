using application.ViewModels;
using FluentValidation;

namespace application.Validators
{
    /// <summary>
    /// Validator pour RegisterViewModel
    /// Valide l'inscription d'un nouvel utilisateur
    /// </summary>
    public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterViewModelValidator()
        {
            // Validation du prénom
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Le prénom est requis")
                .MaximumLength(50)
                .WithMessage("Le prénom ne peut pas dépasser 50 caractères")
                .Matches("^[a-zA-ZÀ-ÿ '-]+$")
                .WithMessage("Le prénom ne peut contenir que des lettres, espaces, apostrophes et tirets");

            // Validation du nom
            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Le nom est requis")
                .MaximumLength(50)
                .WithMessage("Le nom ne peut pas dépasser 50 caractères")
                .Matches("^[a-zA-ZÀ-ÿ '-]+$")
                .WithMessage("Le nom ne peut contenir que des lettres, espaces, apostrophes et tirets");

            // Validation de l'email
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("L'email est requis")
                .EmailAddress()
                .WithMessage("Email invalide")
                .MaximumLength(256)
                .WithMessage("L'email ne peut pas dépasser 256 caractères");

            // Validation du mot de passe
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Le mot de passe est requis")
                .MinimumLength(6)
                .WithMessage("Le mot de passe doit contenir au moins 6 caractères")
                .MaximumLength(100)
                .WithMessage("Le mot de passe ne peut pas dépasser 100 caractères")
                .Must(password => string.IsNullOrEmpty(password) || 
                    (password.Any(char.IsUpper) && 
                     password.Any(char.IsLower) && 
                     password.Any(char.IsDigit) && 
                     password.Any(ch => !char.IsLetterOrDigit(ch))))
                .WithMessage("Le mot de passe doit contenir au moins une majuscule, une minuscule, un chiffre et un caractère spécial");

            // Validation de la confirmation du mot de passe
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("La confirmation du mot de passe est requise")
                .Equal(x => x.Password)
                .WithMessage("Les mots de passe ne correspondent pas");
        }
    }
}
