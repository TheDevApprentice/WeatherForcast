using api.Middleware;
using domain.Constants;
using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Repositories;
using domain.Interfaces.Services;
using domain.Services;
using infra.Data;
using infra.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace api
{
    public class Program
    {
        public static void Main(string[] args)
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
                Console.WriteLine("[API] Using PostgreSQL database with DbContext pooling");

                builder.Services.AddDbContextPool<AppDbContext>(options =>
                    options.UseNpgsql(connectionString),
                    poolSize: 128); // Taille du pool (par défaut: 128)
            }

            // 2. Identity (Authentification)
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // 3. JWT Authentication
            var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "VotreCleSecreteSuperSecuriseeDeMinimum32Caracteres!";
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WeatherForecastApi";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WeatherForecastClient";

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

            // 4. Services (Domain - Logique métier)
            // Nouveaux services séparés (SRP)
            builder.Services.AddScoped<IUserManagementService, UserManagementService>();
            builder.Services.AddScoped<ISessionManagementService, SessionManagementService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Autres services
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IRateLimitService, RateLimitService>();
            builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();
            builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

            // Repositories
            builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

            // Memory Cache pour Rate Limiting
            builder.Services.AddMemoryCache();

            // Authorization - Policies basées sur les permissions
            builder.Services.AddAuthorization(options =>
            {
                // Policies pour les permissions Forecast
                options.AddPolicy(AppClaims.ForecastRead,
                    policy => policy.RequireClaim(AppClaims.Permission, AppClaims.ForecastRead));
                options.AddPolicy(AppClaims.ForecastWrite,
                    policy => policy.RequireClaim(AppClaims.Permission, AppClaims.ForecastWrite));
                options.AddPolicy(AppClaims.ForecastDelete,
                    policy => policy.RequireClaim(AppClaims.Permission, AppClaims.ForecastDelete));
            });

            // 5. Unit of Work (Clean Architecture)
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 6. MediatR pour les Domain Events
            builder.Services.AddMediatR(cfg =>
            {
                // Enregistrer les handlers depuis l'assembly api
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                // Enregistrer les events depuis l'assembly domain
                cfg.RegisterServicesFromAssembly(typeof(WeatherForecastService).Assembly);
            });

            // 7. Redis pour communication inter-process
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
                    logger.LogError(ex, "❌ Erreur lors de la connexion Redis. Les events ne seront pas publiés.");
                    throw; // Ne pas continuer si Redis est critique
                }
            });

            // 8. Controllers
            builder.Services.AddControllers();

            // 7. Swagger/OpenAPI avec API Key (OAuth2 Client Credentials)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "WeatherForecast API - Public REST API",
                    Version = "v1",
                    Description = @"API REST publique en lecture seule avec authentification par clé API (OAuth2 Client Credentials).
                    
**Comment obtenir une clé API :**
1. Créez un compte sur l'application Web : https://weatherforecast.com
2. Connectez-vous et allez dans ""Mes Clés API""
3. Générez une nouvelle clé (Client ID + Client Secret)
4. Utilisez-la avec l'authentification Basic Auth

**Endpoints disponibles :**
- GET /api/weatherforecast - Liste toutes les prévisions
- GET /api/weatherforecast/{id} - Récupère une prévision

**Limites :**
- Lecture seule (pas de POST/PUT/DELETE)
- Rate limiting : 100 requêtes/minute
- Format : JSON uniquement",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "WeatherForecast Support",
                        Email = "support@weatherforecast.com",
                        Url = new Uri("https://weatherforecast.com/support")
                    }
                });

                // Configuration Basic Auth (OAuth2 Client Credentials) dans Swagger
                c.AddSecurityDefinition("Basic", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Basic",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = @"Authentification par clé API (OAuth2 Client Credentials).
                    
Utilisez votre **Client ID** comme username et votre **Client Secret** comme password.

Format : `Basic base64(client_id:client_secret)`

**Exemple avec cURL :**
```
curl -u ""wf_live_xxx:wf_secret_yyy"" https://api.weatherforecast.com/api/weatherforecast
```

**Swagger UI :** Cliquez sur 'Authorize' et entrez votre Client ID et Client Secret."
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Basic"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // 5. CORS (si nécessaire pour appeler depuis Web)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWeb",
                    policy => policy.AllowAnyOrigin()
                                    .AllowAnyHeader()
                                    .AllowAnyMethod());
            });

            // 6. Data Protection : Configuration adaptative (Dev vs Prod)
            var keysDirectory = Path.Combine(Directory.GetCurrentDirectory(), "keys");
            Directory.CreateDirectory(keysDirectory);

            var dataProtectionBuilder = builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
                .SetApplicationName("WeatherForecastApi");

            if (builder.Environment.IsProduction())
            {
                // PRODUCTION : Utiliser un certificat X.509 pour chiffrer les clés
                var certThumbprint = builder.Configuration["DataProtection:CertificateThumbprint"];

                if (!string.IsNullOrEmpty(certThumbprint))
                {
                    dataProtectionBuilder.ProtectKeysWithCertificate(certThumbprint);
                    Console.WriteLine($"[API Production] Data Protection using certificate: {certThumbprint}");
                }
                else
                {
                    Console.WriteLine("[API WARNING] No certificate configured for production. Keys are NOT encrypted!");
                }
            }
            else
            {
                // DÉVELOPPEMENT : Clés en clair dans le dossier local
                Console.WriteLine($"[API Development] Data Protection keys stored in: {keysDirectory}");
            }

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
                Console.WriteLine($"[API] Redis Status: Connected={redis.IsConnected}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Redis initialization failed: {ex.Message}");
            }

            // ============================================
            // CONFIGURATION DU PIPELINE HTTP (Middleware)
            // ============================================

            // 1. Swagger (Dev uniquement)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WeatherForecast API v1");
                });
            }

            // 2. HTTPS redirection
            app.UseHttpsRedirection();

            // 3. CORS
            app.UseCors("AllowWeb");

            // Rate Limiting & Brute Force Protection
            app.UseMiddleware<RateLimitMiddleware>();

            // 4. API Key Authentication (remplace JWT pour l'API publique)
            app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

            app.UseAuthorization();

            // 5. Map Controllers
            app.MapControllers();

            app.Run();
        }
    }
}
