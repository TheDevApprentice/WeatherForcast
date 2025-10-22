using System.ComponentModel.DataAnnotations;

namespace application.ViewModels.Admin
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        public string LastName { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();

        // Claims personnalisés optionnels
        public string? CustomClaimType { get; set; }
        public string? CustomClaimValue { get; set; }
    }
}
