# ========================================
# WeatherForecast Web Application
# Production-ready Dockerfile
# ========================================

# Cet index est utilisé lors de l’exécution à partir de VS en mode rapide (par défaut pour la configuration de débogage)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Créer les répertoires pour les volumes
RUN mkdir -p /app/keys /app/certificates && \
    chmod 755 /app/keys /app/certificates

# Phase build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copier tous les projets (avec dépendances)
COPY ["application/application.csproj", "application/"]
COPY ["domain/domain.csproj", "domain/"]
COPY ["infra/infra.csproj", "infra/"]

# Restore
RUN dotnet restore "application/application.csproj"

# Copier le code source
COPY . .

# Build
WORKDIR "/src/application"
RUN dotnet build "application.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Phase publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "application.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Phase finale
FROM base AS final
WORKDIR /app

# Copier l'application publiée
COPY --from=publish /app/publish .

# Variables d'environnement par défaut
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
ENV ASPNETCORE_URLS=http://+:8080

# Variables d'environnement pour la base de données
ENV POSTGRES_DB=${POSTGRES_DB}
ENV POSTGRES_USER=${POSTGRES_USER}
ENV POSTGRES_PASSWORD=${POSTGRES_PASSWORD}

# Variables d'environnement pour Redis
ENV REDIS_PASSWORD=${REDIS_PASSWORD}

# Volumes pour les clés Data Protection et certificats
VOLUME ["/app/keys", "/app/certificates"]

ENTRYPOINT ["dotnet", "application.dll"]