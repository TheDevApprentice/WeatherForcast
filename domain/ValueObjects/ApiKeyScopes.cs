namespace domain.ValueObjects
{
    /// <summary>
    /// Value Object représentant les scopes (permissions) d'une API Key
    /// Immutable et avec validation intégrée
    /// </summary>
    public record ApiKeyScopes
    {
        private static readonly HashSet<string> ValidScopes = new()
        {
            "read",
            "write",
            "admin"
        };

        // Backing field pour EF Core
        private string _scopesString = "read";

        /// <summary>
        /// Liste des scopes (ex: "read", "write")
        /// </summary>
        public IReadOnlyList<string> Scopes => _scopesString
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList()
            .AsReadOnly();

        /// <summary>
        /// Constructeur principal avec validation
        /// </summary>
        /// <param name="scopes">Liste des scopes séparés par des espaces (ex: "read write")</param>
        public ApiKeyScopes(string scopes)
        {
            if (string.IsNullOrWhiteSpace(scopes))
                throw new ArgumentException("Les scopes ne peuvent pas être vides");

            var scopeList = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s => s.ToLowerInvariant())
                                  .Distinct()
                                  .ToList();

            // Valider que tous les scopes sont valides
            var invalidScopes = scopeList.Where(s => !ValidScopes.Contains(s)).ToList();
            if (invalidScopes.Any())
                throw new ArgumentException(
                    $"Scopes invalides : {string.Join(", ", invalidScopes)}. " +
                    $"Scopes valides : {string.Join(", ", ValidScopes)}");

            _scopesString = string.Join(" ", scopeList);
        }

        /// <summary>
        /// Constructeur depuis une liste de scopes
        /// </summary>
        public ApiKeyScopes(IEnumerable<string> scopes)
            : this(string.Join(" ", scopes))
        {
        }

        /// <summary>
        /// Constructeur parameterless privé pour EF Core
        /// </summary>
        private ApiKeyScopes()
        {
            _scopesString = "read";
        }

        /// <summary>
        /// Vérifie si un scope spécifique est présent
        /// </summary>
        public bool HasScope(string scope)
        {
            return Scopes.Contains(scope.ToLowerInvariant());
        }

        /// <summary>
        /// Vérifie si l'un des scopes est présent
        /// </summary>
        public bool HasAnyScope(params string[] scopes)
        {
            return scopes.Any(s => HasScope(s));
        }

        /// <summary>
        /// Vérifie si tous les scopes sont présents
        /// </summary>
        public bool HasAllScopes(params string[] scopes)
        {
            return scopes.All(s => HasScope(s));
        }

        /// <summary>
        /// Retourne les scopes sous forme de chaîne (pour stockage DB)
        /// </summary>
        public string ToScopeString()
        {
            return _scopesString;
        }

        /// <summary>
        /// Scope par défaut (lecture seule)
        /// </summary>
        public static ApiKeyScopes ReadOnly => new("read");

        /// <summary>
        /// Scope lecture/écriture
        /// </summary>
        public static ApiKeyScopes ReadWrite => new("read write");

        /// <summary>
        /// Scope administrateur (tous les droits)
        /// </summary>
        public static ApiKeyScopes Admin => new("read write admin");

        public override string ToString() => ToScopeString();
    }
}
