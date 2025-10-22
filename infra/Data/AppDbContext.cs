using domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace infra.Data
{
    /// <summary>
    /// DbContext principal de l'application
    /// Configure les entités avec EF Core et Identity
    /// </summary>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<WeatherForecast> WeatherForecasts { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration de WeatherForecast
            modelBuilder.Entity<WeatherForecast>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Configuration explicite de l'auto-incrémentation
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd(); // IDENTITY(1,1) en SQL Server, SERIAL en PostgreSQL
                
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.TemperatureC).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(200);
            });

            // Configuration de Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Configuration explicite de l'auto-génération du Guid
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd(); // Guid auto-généré par la base de données
                
                entity.Property(e => e.Token).IsRequired().HasMaxLength(2000); // JWT peut être long
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                
                entity.HasIndex(e => e.Token).IsUnique();
            });

            // Configuration de UserSession
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Configuration explicite de l'auto-incrémentation
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                
                // Relation User ← UserSession
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserSessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Relation Session ← UserSession
                entity.HasOne(e => e.Session)
                    .WithMany(s => s.UserSessions)
                    .HasForeignKey(e => e.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Index composite pour éviter les doublons
                entity.HasIndex(e => new { e.UserId, e.SessionId }).IsUnique();
            });

            // Configuration de ApiKey
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Configuration explicite de l'auto-incrémentation
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SecretHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Scopes).HasMaxLength(200);
                entity.Property(e => e.AllowedIpAddress).HasMaxLength(50);
                
                // Relation User ← ApiKey
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Index unique sur la clé
                entity.HasIndex(e => e.Key).IsUnique();
                
                // Index sur UserId pour optimiser les recherches
                entity.HasIndex(e => e.UserId);
            });

            // Seed data (données initiales)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Données statiques pour éviter les problèmes de migration
            // (les valeurs dynamiques comme DateTime.Now ou Random causent des erreurs)
            // IMPORTANT: Utiliser DateTimeKind.Utc pour PostgreSQL
            var forecasts = new[]
            {
                new WeatherForecast
                {
                    Id = 1,
                    Date = new DateTime(2025, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                    TemperatureC = 15,
                    Summary = "Cool"
                },
                new WeatherForecast
                {
                    Id = 2,
                    Date = new DateTime(2025, 10, 23, 0, 0, 0, DateTimeKind.Utc),
                    TemperatureC = 22,
                    Summary = "Mild"
                },
                new WeatherForecast
                {
                    Id = 3,
                    Date = new DateTime(2025, 10, 24, 0, 0, 0, DateTimeKind.Utc),
                    TemperatureC = 35,
                    Summary = "Hot"
                },
                new WeatherForecast
                {
                    Id = 4,
                    Date = new DateTime(2025, 10, 25, 0, 0, 0, DateTimeKind.Utc),
                    TemperatureC = -5,
                    Summary = "Freezing"
                },
                new WeatherForecast
                {
                    Id = 5,
                    Date = new DateTime(2025, 10, 26, 0, 0, 0, DateTimeKind.Utc),
                    TemperatureC = 18,
                    Summary = "Warm"
                }
            };

            modelBuilder.Entity<WeatherForecast>().HasData(forecasts);
        }
    }
}
