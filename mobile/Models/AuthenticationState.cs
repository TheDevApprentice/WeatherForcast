namespace mobile.Models
{
    /// <summary>
    /// État d'authentification de l'utilisateur
    /// Sauvegardé en SecureStorage pour éviter les vérifications redondantes
    /// </summary>
    public class AuthenticationState
    {
        public bool IsAuthenticated { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime LastValidated { get; set; }
        public bool SessionValidated { get; set; }

        /// <summary>
        /// Crée un état authentifié
        /// </summary>
        public static AuthenticationState Authenticated(string userId, string email, string firstName, string lastName)
        {
            return new AuthenticationState
            {
                IsAuthenticated = true,
                UserId = userId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                LastValidated = DateTime.UtcNow,
                SessionValidated = true
            };
        }

        /// <summary>
        /// Crée un état non authentifié
        /// </summary>
        public static AuthenticationState Unauthenticated()
        {
            return new AuthenticationState
            {
                IsAuthenticated = false,
                UserId = string.Empty,
                Email = string.Empty,
                FirstName = string.Empty,
                LastName = string.Empty,
                LastValidated = DateTime.MinValue,
                SessionValidated = false
            };
        }

        /// <summary>
        /// Génère les initiales
        /// </summary>
        public string GetInitials()
        {
            var firstInitial = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : "";
            var lastInitial = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpper() : "";
            return $"{firstInitial}{lastInitial}";
        }

        /// <summary>
        /// Nom complet
        /// </summary>
        public string GetFullName()
        {
            return $"{FirstName} {LastName}".Trim();
        }
    }
}
