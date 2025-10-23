using domain.Constants;
using domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace infra.Data
{
    /// <summary>
    /// Seed de 200 utilisateurs de test pour tester les performances
    /// </summary>
    public class UserSeeder
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserSeeder> _logger;

        public UserSeeder(
            UserManager<ApplicationUser> userManager,
            ILogger<UserSeeder> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Créer 200 utilisateurs de test
        /// </summary>
        public async Task SeedTestUsersAsync(int count = 200)
        {
            _logger.LogInformation("Starting seeding {Count} test users...", count);

            var random = new Random();
            var firstNames = new[] { "Jean", "Marie", "Pierre", "Sophie", "Luc", "Emma", "Thomas", "Julie", "Nicolas", "Laura", "Alexandre", "Camille", "Antoine", "Sarah", "Maxime", "Léa", "Hugo", "Chloé", "Lucas", "Manon" };
            var lastNames = new[] { "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy", "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David", "Bertrand", "Roux", "Vincent", "Fournier" };
            var roles = new[] { AppRoles.User, AppRoles.ApiConsumer, AppRoles.MobileUser };

            int created = 0;
            int skipped = 0;

            for (int i = 1; i <= count; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var email = $"user{i}@test.com";

                // Vérifier si l'utilisateur existe déjà
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    skipped++;
                    continue;
                }

                // Créer l'utilisateur
                var user = new ApplicationUser(email, firstName, lastName);

                var result = await _userManager.CreateAsync(user, "Test@123");

                if (result.Succeeded)
                {
                    // Assigner un rôle aléatoire
                    var role = roles[random.Next(roles.Length)];
                    await _userManager.AddToRoleAsync(user, role);

                    // Simuler des dates de création variées (derniers 2 ans)
                    var daysAgo = random.Next(0, 730);
                    // Note: On ne peut pas modifier CreatedAt directement car c'est private set
                    // C'est normal pour l'encapsulation

                    // Simuler des connexions pour certains utilisateurs
                    if (random.Next(100) > 30) // 70% ont déjà connecté
                    {
                        user.RecordLogin();
                    }

                    created++;

                    if (created % 50 == 0)
                    {
                        _logger.LogInformation("Created {Created}/{Total} test users...", created, count);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to create user {Email}: {Errors}",
                        email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            _logger.LogInformation("Test user seeding completed: {Created} created, {Skipped} skipped", created, skipped);
        }

        /// <summary>
        /// Supprimer tous les utilisateurs de test
        /// </summary>
        public async Task DeleteTestUsersAsync()
        {
            _logger.LogInformation("Deleting test users...");

            var testUsers = _userManager.Users
                .Where(u => u.Email!.StartsWith("user") && u.Email.EndsWith("@test.com"))
                .ToList();

            int deleted = 0;

            foreach (var user in testUsers)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    deleted++;
                }
            }

            _logger.LogInformation("Deleted {Count} test users", deleted);
        }
    }
}
