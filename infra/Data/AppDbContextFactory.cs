using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace infra.Data
{
    /// <summary>
    /// Factory pour créer AppDbContext (Design-time support)
    /// Utilisé pour les migrations et les scénarios avancés
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            // Support PostgreSQL (via variable d'environnement) et SQLite
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                // PostgreSQL (pour les migrations Docker)
                Console.WriteLine($"[Migration] Using PostgreSQL");
                optionsBuilder.UseNpgsql(connectionString);
            }

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
