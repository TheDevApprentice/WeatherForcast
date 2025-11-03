namespace mobile.Models
{
    /// <summary>
    /// Représente un profil utilisateur sauvegardé localement pour la reconnexion rapide
    /// </summary>
    public class SavedUserProfile
    {
        /// <summary>
        /// Email de l'utilisateur (identifiant unique)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Prénom de l'utilisateur
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Nom de l'utilisateur
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Date de la dernière connexion réussie
        /// </summary>
        public DateTime LastLoginDate { get; set; }

        /// <summary>
        /// Obtient les initiales de l'utilisateur (ex: "HA" pour Hugo Allard)
        /// </summary>
        public string Initials
        {
            get
            {
                var firstInitial = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : "";
                var lastInitial = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpper() : "";
                return firstInitial + lastInitial;
            }
        }

        /// <summary>
        /// Obtient le nom complet de l'utilisateur
        /// </summary>
        public string FullName => $"{FirstName} {LastName}".Trim();

        /// <summary>
        /// Obtient une couleur générée à partir de l'email pour l'avatar
        /// </summary>
        public Color AvatarColor
        {
            get
            {
                // Générer une couleur basée sur le hash de l'email
                var hash = Email.GetHashCode();
                var colors = new[]
                {
                    Color.FromArgb("#667eea"), // Violet
                    Color.FromArgb("#764ba2"), // Violet foncé
                    Color.FromArgb("#f093fb"), // Rose
                    Color.FromArgb("#4facfe"), // Bleu
                    Color.FromArgb("#43e97b"), // Vert
                    Color.FromArgb("#fa709a"), // Rose-rouge
                    Color.FromArgb("#feca57"), // Jaune
                    Color.FromArgb("#ff6348"), // Rouge-orange
                };

                var index = Math.Abs(hash) % colors.Length;
                return colors[index];
            }
        }

        /// <summary>
        /// Obtient une description relative de la dernière connexion (ex: "Il y a 2 jours")
        /// </summary>
        public string LastLoginDescription
        {
            get
            {
                var timeSpan = DateTime.Now - LastLoginDate;

                if (timeSpan.TotalMinutes < 1)
                    return "À l'instant";
                if (timeSpan.TotalMinutes < 60)
                    return $"Il y a {(int)timeSpan.TotalMinutes} min";
                if (timeSpan.TotalHours < 24)
                    return $"Il y a {(int)timeSpan.TotalHours}h";
                if (timeSpan.TotalDays < 7)
                    return $"Il y a {(int)timeSpan.TotalDays} jour{((int)timeSpan.TotalDays > 1 ? "s" : "")}";
                if (timeSpan.TotalDays < 30)
                    return $"Il y a {(int)(timeSpan.TotalDays / 7)} semaine{((int)(timeSpan.TotalDays / 7) > 1 ? "s" : "")}";
                
                return LastLoginDate.ToString("dd/MM/yyyy");
            }
        }
    }
}
