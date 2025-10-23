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

            // Configuration de ApplicationUser (Identity)
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                // Index pour améliorer les performances de recherche
                entity.HasIndex(e => e.Email).IsUnique(); // Déjà unique par Identity, mais index explicite
                entity.HasIndex(e => e.FirstName);
                entity.HasIndex(e => e.LastName);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastLoginAt);

                // Index composite pour les recherches fréquentes
                entity.HasIndex(e => new { e.IsActive, e.CreatedAt });
                entity.HasIndex(e => new { e.FirstName, e.LastName });
            });

            // Configuration de WeatherForecast
            modelBuilder.Entity<WeatherForecast>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration explicite de l'auto-incrémentation
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd(); // IDENTITY(1,1) en SQL Server, SERIAL en PostgreSQL

                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(200);

                // Configuration du Value Object Temperature (Owned Entity)
                entity.OwnsOne(e => e.Temperature, temperature =>
                {
                    // Mapper la propriété Celsius à la colonne TemperatureC
                    temperature.Property(t => t.Celsius)
                        .HasColumnName("TemperatureC")
                        .IsRequired();

                    // Les propriétés calculées (Fahrenheit, IsHot, IsCold) ne sont pas mappées
                    temperature.Ignore(t => t.Fahrenheit);
                    temperature.Ignore(t => t.IsHot);
                    temperature.Ignore(t => t.IsCold);
                });
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
                entity.Property(e => e.AllowedIpAddress).HasMaxLength(50);
                entity.Property(e => e.RevocationReason).HasMaxLength(500);

                // Configuration du Value Object ApiKeyScopes (Owned Entity)
                entity.OwnsOne(e => e.Scopes, scopes =>
                {
                    // Mapper les scopes vers une colonne texte
                    scopes.Property<string>("_scopesString")
                        .HasColumnName("Scopes")
                        .HasMaxLength(200)
                        .IsRequired();

                    // Ignorer les propriétés calculées
                    scopes.Ignore(s => s.Scopes);
                });

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

            // Seed de 3 semaines de prévisions météo (21 jours)
            var startDate = new DateTime(2025, 10, 22, 0, 0, 0, DateTimeKind.Utc);
            var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };

            // Pour les Owned Entities, EF Core nécessite une configuration spéciale pour le seed
            modelBuilder.Entity<WeatherForecast>().HasData(
                // Semaine 1
                new { Id = 1, Date = startDate, Summary = "Cool" },
                new { Id = 2, Date = startDate.AddDays(1), Summary = "Mild" },
                new { Id = 3, Date = startDate.AddDays(2), Summary = "Warm" },
                new { Id = 4, Date = startDate.AddDays(3), Summary = "Hot" },
                new { Id = 5, Date = startDate.AddDays(4), Summary = "Balmy" },
                new { Id = 6, Date = startDate.AddDays(5), Summary = "Mild" },
                new { Id = 7, Date = startDate.AddDays(6), Summary = "Cool" },

                // Semaine 2
                new { Id = 8, Date = startDate.AddDays(7), Summary = "Chilly" },
                new { Id = 9, Date = startDate.AddDays(8), Summary = "Bracing" },
                new { Id = 10, Date = startDate.AddDays(9), Summary = "Cool" },
                new { Id = 11, Date = startDate.AddDays(10), Summary = "Mild" },
                new { Id = 12, Date = startDate.AddDays(11), Summary = "Warm" },
                new { Id = 13, Date = startDate.AddDays(12), Summary = "Balmy" },
                new { Id = 14, Date = startDate.AddDays(13), Summary = "Hot" },

                // Semaine 3
                new { Id = 15, Date = startDate.AddDays(14), Summary = "Sweltering" },
                new { Id = 16, Date = startDate.AddDays(15), Summary = "Hot" },
                new { Id = 17, Date = startDate.AddDays(16), Summary = "Warm" },
                new { Id = 18, Date = startDate.AddDays(17), Summary = "Mild" },
                new { Id = 19, Date = startDate.AddDays(18), Summary = "Cool" },
                new { Id = 20, Date = startDate.AddDays(19), Summary = "Chilly" },
                new { Id = 21, Date = startDate.AddDays(20), Summary = "Freezing" }
            );

            // Seed des Value Objects Temperature (Owned Entity)
            modelBuilder.Entity<WeatherForecast>()
                .OwnsOne(w => w.Temperature)
                .HasData(
                    // Semaine 1 (températures progressives)
                    new { WeatherForecastId = 1, Celsius = 15 },
                    new { WeatherForecastId = 2, Celsius = 18 },
                    new { WeatherForecastId = 3, Celsius = 22 },
                    new { WeatherForecastId = 4, Celsius = 28 },
                    new { WeatherForecastId = 5, Celsius = 25 },
                    new { WeatherForecastId = 6, Celsius = 20 },
                    new { WeatherForecastId = 7, Celsius = 16 },

                    // Semaine 2 (températures variables)
                    new { WeatherForecastId = 8, Celsius = 12 },
                    new { WeatherForecastId = 9, Celsius = 8 },
                    new { WeatherForecastId = 10, Celsius = 14 },
                    new { WeatherForecastId = 11, Celsius = 19 },
                    new { WeatherForecastId = 12, Celsius = 23 },
                    new { WeatherForecastId = 13, Celsius = 26 },
                    new { WeatherForecastId = 14, Celsius = 30 },

                    // Semaine 3 (températures décroissantes)
                    new { WeatherForecastId = 15, Celsius = 35 },
                    new { WeatherForecastId = 16, Celsius = 32 },
                    new { WeatherForecastId = 17, Celsius = 27 },
                    new { WeatherForecastId = 18, Celsius = 21 },
                    new { WeatherForecastId = 19, Celsius = 17 },
                    new { WeatherForecastId = 20, Celsius = 10 },
                    new { WeatherForecastId = 21, Celsius = 2 }
                );
        }
    }
}
