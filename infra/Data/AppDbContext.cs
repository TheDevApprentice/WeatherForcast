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
            
            // Pour les Owned Entities, EF Core nécessite une configuration spéciale pour le seed
            modelBuilder.Entity<WeatherForecast>().HasData(
                new { Id = 1, Date = new DateTime(2025, 10, 22, 0, 0, 0, DateTimeKind.Utc), Summary = "Cool" },
                new { Id = 2, Date = new DateTime(2025, 10, 23, 0, 0, 0, DateTimeKind.Utc), Summary = "Mild" },
                new { Id = 3, Date = new DateTime(2025, 10, 24, 0, 0, 0, DateTimeKind.Utc), Summary = "Hot" },
                new { Id = 4, Date = new DateTime(2025, 10, 25, 0, 0, 0, DateTimeKind.Utc), Summary = "Freezing" },
                new { Id = 5, Date = new DateTime(2025, 10, 26, 0, 0, 0, DateTimeKind.Utc), Summary = "Warm" }
            );

            // Seed des Value Objects Temperature (Owned Entity)
            modelBuilder.Entity<WeatherForecast>()
                .OwnsOne(w => w.Temperature)
                .HasData(
                    new { WeatherForecastId = 1, Celsius = 15 },
                    new { WeatherForecastId = 2, Celsius = 22 },
                    new { WeatherForecastId = 3, Celsius = 35 },
                    new { WeatherForecastId = 4, Celsius = -5 },
                    new { WeatherForecastId = 5, Celsius = 18 }
                );
        }
    }
}
