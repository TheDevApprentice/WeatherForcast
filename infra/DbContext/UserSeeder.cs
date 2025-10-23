using domain.Constants;
using domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace infra.Data
{
    /// <summary>
    /// Seed optimisé avec traitement parallèle pour créer rapidement des utilisateurs de test
    /// </summary>
    public class UserSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserSeeder> _logger;

        public UserSeeder(
            IServiceProvider serviceProvider,
            ILogger<UserSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Créer des utilisateurs de test en parallèle ULTRA-OPTIMISÉ (pour 1000+ utilisateurs)
        /// </summary>
        public async Task SeedTestUsersAsync(int count = 200)
        {
            var stopwatch = Stopwatch.StartNew();
            //_logger.LogInformation("🚀 Starting ULTRA-FAST parallel seeding of {Count} test users...", count);

            var firstNames = new[] { "Jean", "Marie", "Pierre", "Sophie", "Luc", "Emma", "Thomas", "Julie", "Nicolas", "Laura", "Alexandre", "Camille", "Antoine", "Sarah", "Maxime", "Léa", "Hugo", "Chloé", "Lucas", "Manon" };
            var lastNames = new[] { "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard", "Petit", "Durand", "Leroy", "Moreau", "Simon", "Laurent", "Lefebvre", "Michel", "Garcia", "David", "Bertrand", "Roux", "Vincent", "Fournier" };
            var roles = new[] { AppRoles.User, AppRoles.ApiConsumer, AppRoles.MobileUser };

            var createdCounter = new ConcurrentBag<bool>();
            var skippedCounter = new ConcurrentBag<bool>();

            // OPTIMISATION CLÉS:
            // 1. Batch plus grand (100 au lieu de 50) = moins de synchronisation
            // 2. MaxDegreeOfParallelism basé sur les cores CPU
            // 3. Pas de vérification d'existence (on assume DB vide)
            const int batchSize = 100;
            var maxParallelism = Environment.ProcessorCount * 2; // 2x le nombre de cores
            var batches = (int)Math.Ceiling(count / (double)batchSize);

            //_logger.LogInformation("⚙️ Configuration: {Batches} batches of {BatchSize}, {Parallelism} max parallelism", 
            //    batches, batchSize, maxParallelism);

            for (int batchIndex = 0; batchIndex < batches; batchIndex++)
            {
                var start = batchIndex * batchSize + 1;
                var end = Math.Min((batchIndex + 1) * batchSize, count);

                //_logger.LogInformation("⚡ Processing batch {Batch}/{Total} ({Start}-{End})...", 
                //    batchIndex + 1, batches, start, end);

                // Utiliser Parallel.ForEachAsync pour un contrôle optimal du parallélisme
                var userIndices = Enumerable.Range(start, end - start + 1);

                await Parallel.ForEachAsync(userIndices,
                    new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
                    async (userIndex, cancellationToken) =>
                    {
                        try
                        {
                            // Chaque tâche a son propre scope
                            using var scope = _serviceProvider.CreateScope();
                            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                            var random = new Random(Guid.NewGuid().GetHashCode());

                            var firstName = firstNames[random.Next(firstNames.Length)];
                            var lastName = lastNames[random.Next(lastNames.Length)];
                            var email = $"user{userIndex}@test.com";

                            // OPTIMISATION: Skip la vérification d'existence pour la vitesse
                            // (on assume que la DB est vide ou qu'on veut recréer)

                            // Créer l'utilisateur
                            var user = new ApplicationUser(email, firstName, lastName);
                            var result = await userManager.CreateAsync(user, "Test@123");

                            if (result.Succeeded)
                            {
                                // Assigner un rôle aléatoire
                                var role = roles[random.Next(roles.Length)];
                                await userManager.AddToRoleAsync(user, role);

                                // Simuler des connexions pour certains utilisateurs (70%)
                                if (random.Next(100) > 30)
                                {
                                    user.RecordLogin();
                                    await userManager.UpdateAsync(user);
                                }

                                createdCounter.Add(true);
                            }
                            else
                            {
                                // Si l'utilisateur existe déjà, on le compte comme skipped
                                if (result.Errors.Any(e => e.Code == "DuplicateUserName"))
                                {
                                    skippedCounter.Add(true);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error creating user{Index}", userIndex);
                        }
                    });

                var created = createdCounter.Count;
                var skipped = skippedCounter.Count;

                //_logger.LogInformation("✅ Batch {Batch}/{Total} completed - Total: {Created} created, {Skipped} skipped ({Rate:F0} users/sec)", 
                //    batchIndex + 1, batches, created, skipped, created / stopwatch.Elapsed.TotalSeconds);
            }

            stopwatch.Stop();
            var rate = createdCounter.Count / stopwatch.Elapsed.TotalSeconds;
            _logger.LogInformation("🎉 ULTRA-FAST seeding completed in {Elapsed:F2}s: {Created} created, {Skipped} skipped ({Rate:F0} users/sec)",
                stopwatch.Elapsed.TotalSeconds, createdCounter.Count, skippedCounter.Count, rate);
        }
    }
}
