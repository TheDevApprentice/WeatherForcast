using domain.Interfaces;
using domain.Interfaces.Services;
using infra.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace application
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configuration Npgsql : Convertir automatiquement les DateTime en UTC
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            // ============================================
            // CONFIGURATION DES SERVICES (Dependency Injection)
            // ============================================

            // 1. DbContext - Support PostgreSQL (Docker) et SQLite (Local)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                // PostgreSQL (Docker ou production) avec Connection Pooling
                Console.WriteLine("[Web] Using PostgreSQL database with DbContext pooling");

                builder.Services.AddDbContextPool<AppDbContext>(options =>
                    options.UseNpgsql(connectionString),
                    poolSize: 128); // Taille du pool (par défaut: 128)
            }

            // 2. Identity (Authentification)
            builder.Services.AddIdentity<domain.Entities.ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
            {
                // Configuration du mot de passe
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // Configuration du compte
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;

                // Configuration du lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Configuration des cookies d'authentification
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            // Data Protection : Configuration adaptative (Dev vs Prod)
            var keysDirectory = Path.Combine(Directory.GetCurrentDirectory(), "keys");
            Directory.CreateDirectory(keysDirectory);

            var dataProtectionBuilder = builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
                .SetApplicationName("WeatherForecastApp");

            if (builder.Environment.IsProduction())
            {
                // PRODUCTION : Utiliser un certificat X.509 pour chiffrer les clés
                var certThumbprint = builder.Configuration["DataProtection:CertificateThumbprint"];

                if (!string.IsNullOrEmpty(certThumbprint))
                {
                    dataProtectionBuilder.ProtectKeysWithCertificate(certThumbprint);
                    Console.WriteLine($"[Production] Data Protection using certificate: {certThumbprint}");
                }
                else
                {
                    Console.WriteLine("[WARNING] No certificate configured for production. Keys are NOT encrypted!");
                }
            }
            else
            {
                // DÉVELOPPEMENT : Clés en clair dans le dossier local
                Console.WriteLine($"[Development] Data Protection keys stored in: {keysDirectory}");
            }

            // 3. Services (Domain - Logique métier)
            // Services séparés (SRP - Single Responsibility Principle)
            builder.Services.AddScoped<IUserManagementService, domain.Services.UserManagementService>();
            builder.Services.AddScoped<ISessionManagementService, domain.Services.SessionManagementService>();
            builder.Services.AddScoped<IAuthenticationService, domain.Services.AuthenticationService>();
            builder.Services.AddScoped<IRoleManagementService, domain.Services.RoleManagementService>();

            // Autres services
            builder.Services.AddScoped<IRateLimitService, domain.Services.RateLimitService>();
            builder.Services.AddScoped<IWeatherForecastService, domain.Services.WeatherForecastService>();
            builder.Services.AddScoped<IApiKeyService, domain.Services.ApiKeyService>();

            // Repositories
            builder.Services.AddScoped<domain.Interfaces.Repositories.IApiKeyRepository, infra.Repositories.ApiKeyRepository>();

            // Memory Cache pour Rate Limiting
            builder.Services.AddMemoryCache();

            // Authorization - Policies basées sur les permissions
            builder.Services.AddAuthorization(options =>
            {
                // Policies pour les permissions Forecast
                options.AddPolicy(domain.Constants.AppClaims.ForecastRead,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.ForecastRead)));
                options.AddPolicy(domain.Constants.AppClaims.ForecastWrite,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.ForecastWrite)));
                options.AddPolicy(domain.Constants.AppClaims.ForecastDelete,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.ForecastDelete)));

                // Policies pour les permissions API Key
                options.AddPolicy(domain.Constants.AppClaims.ApiKeyManage,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.ApiKeyManage)));
                options.AddPolicy(domain.Constants.AppClaims.ApiKeyViewAll,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.ApiKeyViewAll)));

                // Policies pour les permissions User
                options.AddPolicy(domain.Constants.AppClaims.UserManage,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.UserManage)));
                options.AddPolicy(domain.Constants.AppClaims.UserViewAll,
                    policy => policy.Requirements.Add(new application.Authorization.PermissionRequirement(domain.Constants.AppClaims.UserViewAll)));
            });

            // Authorization Handler
            builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, application.Authorization.PermissionHandler>();

            // 4. Unit of Work (Clean Architecture)
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 5. MediatR pour les Domain Events (notifications temps réel, audit logs, etc.)
            builder.Services.AddMediatR(cfg =>
            {
                // Enregistrer les handlers depuis l'assembly application
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                // Enregistrer les events depuis l'assembly domain
                cfg.RegisterServicesFromAssembly(typeof(domain.Services.WeatherForecastService).Assembly);
            });

            // 6. Redis pour communication inter-process
            var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
            builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();

                var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString!);
                configuration.AbortOnConnectFail = false; // Ne pas planter si Redis est indisponible
                configuration.ConnectTimeout = 15000;      // 15 secondes
                configuration.SyncTimeout = 5000;          // 5 secondes pour les opérations
                configuration.ConnectRetry = 5;            // 5 tentatives
                configuration.KeepAlive = 60;              // Keep-alive toutes les 60 secondes

                try
                {
                    logger.LogInformation("🔄 Connexion à Redis: {Endpoint}...", redisConnectionString);
                    var connection = StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);

                    // Attendre un peu que la connexion soit établie
                    var attempts = 0;
                    while (!connection.IsConnected && attempts < 10)
                    {
                        System.Threading.Thread.Sleep(500);
                        attempts++;
                    }

                    if (connection.IsConnected)
                    {
                        logger.LogInformation("✅ Connecté à Redis: {Endpoint}", redisConnectionString);
                    }
                    else
                    {
                        logger.LogWarning("⚠️ Redis : Connexion créée mais pas encore établie. Retry en arrière-plan...");
                    }

                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Erreur lors de la connexion Redis. Le subscriber ne fonctionnera pas.");
                    throw; // Ne pas continuer si Redis est critique
                }
            });

            // 7. BackgroundService pour écouter les events Redis
            builder.Services.AddHostedService<application.BackgroundServices.RedisSubscriberService>();

            // 8. MVC
            builder.Services.AddControllersWithViews();

            // 9. SignalR pour les notifications en temps réel
            builder.Services.AddSignalR();

            var app = builder.Build();

            // ============================================
            // INITIALISATION DE LA BASE DE DONNÉES
            // ============================================
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // S'assurer que la base de données est créée avec les données seed
                context.Database.EnsureCreated();
            }

            // ============================================
            // INITIALISATION DE REDIS
            // ============================================
            // Forcer la résolution du IConnectionMultiplexer pour établir la connexion au démarrage
            try
            {
                var redis = app.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
                Console.WriteLine($"[Web] Redis Status: Connected={redis.IsConnected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Web] Redis initialization failed: {ex.Message}");
            }

            // ============================================
            // CONFIGURATION DU PIPELINE HTTP (Middleware)
            // ============================================

            // 1. Exception handling
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            // 2. HTTPS redirection
            app.UseHttpsRedirection();

            // 3. Fichiers statiques (wwwroot)
            app.UseStaticFiles();

            // 4. Routing
            app.UseRouting();

            // Rate Limiting & Brute Force Protection
            app.UseMiddleware<application.Middleware.RateLimitMiddleware>();

            // 5. Authentication & Authorization
            app.UseAuthentication();

            // Middleware de validation de session (vérifie si la session existe toujours en DB)
            app.UseMiddleware<application.Middleware.SessionValidationMiddleware>();

            app.UseAuthorization();

            // 6. Endpoints MVC
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // 7. SignalR Hub
            app.MapHub<application.Hubs.WeatherForecastHub>("/hubs/weatherforecast");

            // ============================================
            // SEED DES RÔLES ET UTILISATEUR ADMIN
            // ============================================
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<domain.Entities.ApplicationUser>>();
                    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<infra.Data.RoleSeeder>();

                    var roleSeeder = new infra.Data.RoleSeeder(roleManager, logger);

                    // Créer les rôles avec leurs claims
                    await roleSeeder.SeedRolesAsync();

                    // Créer l'utilisateur admin par défaut
                    await roleSeeder.SeedAdminUserAsync(userManager);

                    Console.WriteLine("✅ Roles and admin user seeded successfully");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding roles");
                }
            }

            app.Run();
        }
    }
}
