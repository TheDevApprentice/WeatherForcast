using domain.Constants;

namespace domain.ValueObjects
{
    /// <summary>
    /// Value Object représentant les scopes (permissions) d'une API Key
    /// Utilise les mêmes permissions que les Claims pour cohérence
    /// Immutable et avec validation intégrée
    /// </summary>
    public record ApiKeyScopes
    {
        /// <summary>
        /// Scopes valides (alignés avec AppClaims)
        /// </summary>
        private static readonly HashSet<string> ValidScopes = new()
        {
            AppClaims.ForecastRead,
            AppClaims.ForecastWrite,
            AppClaims.ForecastDelete
        };

        // Backing field pour EF Core (séparateur: espace)
        private string _scopesString = AppClaims.ForecastRead;

        /// <summary>
        /// Liste des scopes (ex: "forecast:read", "forecast:write")
        /// </summary>
        public IReadOnlyList<string> Scopes => _scopesString
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList()
            .AsReadOnly();

        /// <summary>
        /// Constructeur principal avec validation
        /// </summary>
        /// <param name="scopes">Liste des scopes séparés par des espaces (ex: "forecast:read forecast:write")</param>
        public ApiKeyScopes(string scopes)
        {
            if (string.IsNullOrWhiteSpace(scopes))
                throw new ArgumentException("Les scopes ne peuvent pas être vides");

            var scopeList = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
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
            _scopesString = AppClaims.ForecastRead;
        }

        /// <summary>
        /// Vérifie si un scope spécifique est présent
        /// </summary>
        public bool HasScope(string scope)
        {
            return Scopes.Contains(scope);
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
        public static ApiKeyScopes ReadOnly => new(AppClaims.ForecastRead);

        /// <summary>
        /// Scope lecture/écriture
        /// </summary>
        public static ApiKeyScopes ReadWrite => new($"{AppClaims.ForecastRead} {AppClaims.ForecastWrite}");

        /// <summary>
        /// Scope complet (lecture, écriture, suppression)
        /// </summary>
        public static ApiKeyScopes FullAccess => new($"{AppClaims.ForecastRead} {AppClaims.ForecastWrite} {AppClaims.ForecastDelete}");

        public override string ToString() => ToScopeString();
    }
}
