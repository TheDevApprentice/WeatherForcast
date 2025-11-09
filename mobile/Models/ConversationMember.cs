namespace mobile.Models
{
    /// <summary>
    /// Modèle représentant un membre d'une conversation
    /// </summary>
    public class ConversationMember
    {
        /// <summary>
        /// Identifiant de l'utilisateur
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Nom d'affichage de l'utilisateur
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Prénom de l'utilisateur
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Nom de famille de l'utilisateur
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// URL de l'avatar de l'utilisateur (optionnel)
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Rôle dans la conversation (Member, Admin, Support)
        /// </summary>
        public ConversationRole Role { get; set; } = ConversationRole.Member;

        /// <summary>
        /// Date d'ajout à la conversation
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Obtient les initiales de l'utilisateur
        /// </summary>
        public string GetInitials()
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                return $"{FirstName[0]}{LastName[0]}".ToUpper();

            if (!string.IsNullOrEmpty(DisplayName))
            {
                var words = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                    return $"{words[0][0]}{words[1][0]}".ToUpper();
                if (words.Length == 1 && words[0].Length >= 2)
                    return words[0].Substring(0, 2).ToUpper();
                if (words.Length == 1 && words[0].Length == 1)
                    return words[0][0].ToString().ToUpper();
            }

            return "?";
        }
    }

    /// <summary>
    /// Rôle d'un membre dans une conversation
    /// </summary>
    public enum ConversationRole
    {
        /// <summary>
        /// Membre standard
        /// </summary>
        Member,

        /// <summary>
        /// Administrateur de la conversation
        /// </summary>
        Admin,

        /// <summary>
        /// Agent de support
        /// </summary>
        Support
    }
}
