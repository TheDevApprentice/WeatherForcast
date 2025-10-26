using System.ComponentModel.DataAnnotations;

namespace application.ViewModels
{
    /// <summary>
    /// ViewModel pour l'inscription d'un utilisateur
    /// Validation déléguée à FluentValidation (RegisterViewModelValidator)
    /// </summary>
    public class RegisterViewModel
    {
        [Display(Name = "Prénom")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Nom")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
