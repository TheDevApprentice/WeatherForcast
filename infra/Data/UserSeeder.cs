//using domain.Constants;
//using domain.Entities;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Logging;

//namespace infra.Data
//{
//    /// <summary>
//    /// Seeder pour créer des utilisateurs de test
//    /// </summary>
//    public class UserSeeder
//    {
//        private readonly UserManager<ApplicationUser> _userManager;
//        private readonly ILogger<UserSeeder> _logger;

//        // Listes de données pour générer des noms variés
//        private readonly string[] _firstNames = new[]
//        {
//            "Alexandre", "Benjamin", "Charlotte", "David", "Emma", "François", "Gabriel", "Hugo",
//            "Isabelle", "Julien", "Kevin", "Laura", "Marie", "Nicolas", "Olivia", "Pierre",
//            "Quentin", "Rachel", "Sophie", "Thomas", "Valérie", "William", "Xavier", "Yasmine", "Zoé",
//            "Antoine", "Brigitte", "Camille", "Damien", "Élise", "Fabien", "Géraldine", "Henri",
//            "Inès", "Jacques", "Karine", "Louis", "Margot", "Nathan", "Océane", "Paul",
//            "Raphaël", "Sarah", "Théo", "Victoire", "Yves", "Amélie", "Baptiste", "Céline", "Denis"
//        };

//        private readonly string[] _lastNames = new[]
//        {
//            "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand",
//            "Leroy", "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David",
//            "Bertrand", "Roux", "Vincent", "Fournier", "Morel", "Girard", "André", "Mercier",
//            "Dupont", "Lambert", "Bonnet", "François", "Martinez", "Legrand", "Garnier", "Faure",
//            "Rousseau", "Blanc", "Guerin", "Muller", "Henry", "Roussel", "Nicolas", "Perrin",
//            "Morin", "Mathieu", "Clement", "Gauthier", "Dumont", "Lopez", "Fontaine", "Chevalier",
//            "Robin", "Masson"
//        };

//        private readonly string[] _roles = new[]
//        {
//            AppRoles.User,
//            AppRoles.MobileUser
//        };

//        public UserSeeder(UserManager<ApplicationUser> userManager, ILogger<UserSeeder> logger)
//        {
//            _userManager = userManager;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Créer 100 utilisateurs de test
//        /// </summary>
//        public async Task SeedUsersAsync()
//        {
//            _logger.LogInformation("Starting user seeding...");

//            var random = new Random(42); // Seed fixe pour reproductibilité
//            var createdCount = 0;
//            var skippedCount = 0;

//            for (int i = 1; i <= 100; i++)
//            {
//                // Générer des données aléatoires
//                var firstName = _firstNames[random.Next(_firstNames.Length)];
//                var lastName = _lastNames[random.Next(_lastNames.Length)];
//                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@test.com";

//                // Vérifier si l'utilisateur existe déjà
//                var existingUser = await _userManager.FindByEmailAsync(email);
//                if (existingUser != null)
//                {
//                    skippedCount++;
//                    continue;
//                }

//                // Créer l'utilisateur
//                var user = new ApplicationUser(email, firstName, lastName);

//                // Définir une date de création aléatoire (entre 1 an et aujourd'hui)
//                var daysAgo = random.Next(1, 365);
//                user.SetCreatedAtForSeeding(DateTime.UtcNow.AddDays(-daysAgo));

//                // 80% des utilisateurs sont actifs
//                if (random.Next(100) < 20)
//                {
//                    user.Deactivate("Compte de test désactivé");
//                }

//                // 70% des utilisateurs se sont déjà connectés
//                if (random.Next(100) < 70)
//                {
//                    var lastLoginDaysAgo = random.Next(0, daysAgo);
//                    user.SetLastLoginAtForSeeding(DateTime.UtcNow.AddDays(-lastLoginDaysAgo));
//                }

//                // Créer l'utilisateur avec un mot de passe par défaut
//                var result = await _userManager.CreateAsync(user, "Test@123");

//                if (result.Succeeded)
//                {
//                    // Assigner un rôle aléatoire
//                    var role = _roles[random.Next(_roles.Length)];
//                    await _userManager.AddToRoleAsync(user, role);

//                    // 30% des utilisateurs ont plusieurs rôles
//                    if (random.Next(100) < 30)
//                    {
//                        var secondRole = _roles[random.Next(_roles.Length)];
//                        if (secondRole != role)
//                        {
//                            await _userManager.AddToRoleAsync(user, secondRole);
//                        }
//                    }

//                    // 20% des utilisateurs ont un claim personnalisé
//                    if (random.Next(100) < 20)
//                    {
//                        var departments = new[] { "IT", "HR", "Sales", "Marketing", "Finance" };
//                        var department = departments[random.Next(departments.Length)];
//                        await _userManager.AddClaimAsync(user,
//                            new System.Security.Claims.Claim("department", department));
//                    }

//                    createdCount++;

//                    if (createdCount % 10 == 0)
//                    {
//                        _logger.LogInformation("Created {Count} users...", createdCount);
//                    }
//                }
//                else
//                {
//                    _logger.LogWarning("Failed to create user {Email}: {Errors}",
//                        email, string.Join(", ", result.Errors.Select(e => e.Description)));
//                    skippedCount++;
//                }
//            }

//            _logger.LogInformation(
//                "User seeding completed: {Created} created, {Skipped} skipped",
//                createdCount, skippedCount);
//        }

//        /// <summary>
//        /// Créer des utilisateurs spécifiques pour les tests
//        /// </summary>
//        public async Task SeedTestUsersAsync()
//        {
//            _logger.LogInformation("Creating specific test users...");

//            // Utilisateur actif avec User role
//            await CreateTestUserAsync("active.user@test.com", "Active", "User",
//                AppRoles.User, isActive: true, hasLoggedIn: true);

//            // Utilisateur inactif
//            await CreateTestUserAsync("inactive.user@test.com", "Inactive", "User",
//                AppRoles.User, isActive: false, hasLoggedIn: false);

//            // Utilisateur mobile
//            await CreateTestUserAsync("mobile.user@test.com", "Mobile", "User",
//                AppRoles.MobileUser, isActive: true, hasLoggedIn: true);

//            // Utilisateur API
//            await CreateTestUserAsync("api.consumer@test.com", "API", "Consumer",
//                AppRoles.ApiConsumer, isActive: true, hasLoggedIn: true);

//            // Utilisateur avec plusieurs rôles
//            var multiRoleUser = await CreateTestUserAsync("multi.role@test.com", "Multi", "Role",
//                AppRoles.User, isActive: true, hasLoggedIn: true);
//            if (multiRoleUser != null)
//            {
//                await _userManager.AddToRoleAsync(multiRoleUser, AppRoles.MobileUser);
//            }

//            _logger.LogInformation("Specific test users created successfully");
//        }

//        private async Task<ApplicationUser?> CreateTestUserAsync(
//            string email,
//            string firstName,
//            string lastName,
//            string role,
//            bool isActive,
//            bool hasLoggedIn)
//        {
//            var existingUser = await _userManager.FindByEmailAsync(email);
//            if (existingUser != null)
//            {
//                return existingUser;
//            }

//            var user = new ApplicationUser(email, firstName, lastName);

//            if (!isActive)
//            {
//                user.Deactivate("Test user - inactive");
//            }

//            if (hasLoggedIn)
//            {
//                user.SetLastLoginAtForSeeding(DateTime.UtcNow.AddDays(-7));
//            }

//            var result = await _userManager.CreateAsync(user, "Test@123");

//            if (result.Succeeded)
//            {
//                await _userManager.AddToRoleAsync(user, role);
//                _logger.LogInformation("Created test user: {Email}", email);
//                return user;
//            }

//            _logger.LogWarning("Failed to create test user {Email}", email);
//            return null;
//        }
//    }
//}
