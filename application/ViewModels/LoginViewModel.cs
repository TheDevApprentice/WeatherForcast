using System.ComponentModel.DataAnnotations;

namespace application.ViewModels
{
    /// <summary>
    /// ViewModel pour la connexion d'un utilisateur
    /// Validation déléguée à FluentValidation (LoginViewModelValidator)
    /// </summary>
    public class LoginViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }
}
