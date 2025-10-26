using System.ComponentModel.DataAnnotations;

namespace application.ViewModels.Admin
{
    /// <summary>
    /// ViewModel pour la création d'un utilisateur par un administrateur
    /// Validation déléguée à FluentValidation (CreateUserViewModelValidator)
    /// </summary>
    public class CreateUserViewModel
    {
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();

        // Claims personnalisés optionnels
        public string? CustomClaimType { get; set; }
        public string? CustomClaimValue { get; set; }
    }
}
