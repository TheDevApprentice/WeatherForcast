using application.ViewModels.Admin;
using FluentValidation;

namespace application.Validators
{
    /// <summary>
    /// Validator pour CreateUserViewModel (Admin)
    /// Valide la création d'un utilisateur par un administrateur
    /// </summary>
    public class CreateUserViewModelValidator : AbstractValidator<CreateUserViewModel>
    {
        public CreateUserViewModelValidator()
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
                .WithMessage("Format d'email invalide")
                .MaximumLength(256)
                .WithMessage("L'email ne peut pas dépasser 256 caractères");

            // Validation du mot de passe
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Le mot de passe est requis")
                .MinimumLength(6)
                .WithMessage("Le mot de passe doit contenir au moins 6 caractères")
                .MaximumLength(100)
                .WithMessage("Le mot de passe ne peut pas dépasser 100 caractères");

            // Validation des rôles sélectionnés
            RuleFor(x => x.SelectedRoles)
                .NotEmpty()
                .WithMessage("Au moins un rôle doit être sélectionné")
                .Must(roles => roles != null && roles.Count > 0)
                .WithMessage("Au moins un rôle doit être sélectionné");

            // Validation des claims personnalisés (optionnels mais cohérents)
            RuleFor(x => x.CustomClaimType)
                .NotEmpty()
                .When(x => !string.IsNullOrWhiteSpace(x.CustomClaimValue))
                .WithMessage("Le type de claim est requis si une valeur est fournie");

            RuleFor(x => x.CustomClaimValue)
                .NotEmpty()
                .When(x => !string.IsNullOrWhiteSpace(x.CustomClaimType))
                .WithMessage("La valeur du claim est requise si un type est fourni");
        }
    }
}
