#!/bin/bash
# ========================================
# Script pour démarrer PostgreSQL (Docker) et créer les migrations
# ========================================

set -e  # Exit on error

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

echo -e "${CYAN}"
echo "========================================"
echo "  SETUP BASE DE DONNÉES POSTGRESQL"
echo "========================================"
echo -e "${NC}"

# 1. Vérifier que Docker est installé et démarré
echo -e "${YELLOW}[1/5] Vérification de Docker...${NC}"
if ! docker info >/dev/null 2>&1; then
    echo -e "${RED}[ERREUR] Docker n'est pas démarré ou installé${NC}"
    echo -e "${YELLOW}Veuillez démarrer Docker et réessayer${NC}"
    exit 1
fi
echo -e "${GREEN}   [OK] Docker est prêt${NC}"

# 2. Arrêter et supprimer les anciens containers/volumes
echo -e "\n${YELLOW}[2/5] Nettoyage des anciens containers...${NC}"
docker-compose down -v 2>/dev/null || true

# 3. Démarrer le container PostgreSQL
echo -e "\n${YELLOW}[3/5] Démarrage du container PostgreSQL...${NC}"
if docker-compose up -d postgres; then
    echo -e "${GREEN}   [OK] Container PostgreSQL démarré${NC}"
else
    echo -e "${RED}   [ERREUR] Échec du démarrage du container${NC}"
    exit 1
fi

# 4. Attendre que PostgreSQL soit prêt
echo -e "\n${YELLOW}[4/5] Attente que PostgreSQL soit prêt...${NC}"
MAX_RETRIES=30
RETRIES=0

while [ $RETRIES -lt $MAX_RETRIES ]; do
    HEALTH_CHECK=$(docker inspect --format='{{.State.Health.Status}}' weatherforecast-db 2>/dev/null || echo "unknown")
    
    if [ "$HEALTH_CHECK" = "healthy" ]; then
        echo -e "${GREEN}   [OK] PostgreSQL est prêt${NC}"
        break
    fi
    
    RETRIES=$((RETRIES + 1))
    echo -e "${GRAY}   Attente... ($RETRIES/$MAX_RETRIES)${NC}"
    sleep 2
done

if [ $RETRIES -eq $MAX_RETRIES ]; then
    echo -e "${RED}   [ERREUR] Timeout: PostgreSQL n'est pas devenu prêt${NC}"
    exit 1
fi

# 5. Installer dotnet-ef et créer les migrations
echo -e "\n${YELLOW}[5/6] Création des migrations...${NC}"

# Déterminer le chemin du script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR/../infra"

echo -e "${GRAY}   Installation de dotnet-ef (si nécessaire)...${NC}"
if ! dotnet tool install --global dotnet-ef 2>/dev/null; then
    echo -e "${GRAY}   dotnet-ef déjà installé${NC}"
fi

echo -e "${GRAY}   Suppression des anciennes migrations...${NC}"
rm -rf ./Data/Migrations 2>/dev/null || true

echo -e "${GRAY}   Vérification du build...${NC}"
if ! dotnet build; then
    echo -e "${RED}   [ERREUR] Erreurs de compilation détectées ci-dessus${NC}"
    cd "$SCRIPT_DIR"
    exit 1
fi

echo -e "${GRAY}   Création de la migration initiale...${NC}"
export ConnectionStrings__DefaultConnection="Host=localhost;Database=weatherforecastdb;Username=weatheruser;Password=weatherpass"

if dotnet ef migrations add InitialCreate --output-dir Data/Migrations; then
    echo -e "${GREEN}   [OK] Migration créée avec succès${NC}"
else
    echo -e "${RED}   [ERREUR] Échec de la création de la migration${NC}"
    cd "$SCRIPT_DIR"
    exit 1
fi

# 6. Appliquer les migrations
echo -e "\n${YELLOW}[6/6] Application des migrations sur PostgreSQL...${NC}"
if dotnet ef database update; then
    echo -e "${GREEN}   [OK] Base de données PostgreSQL créée avec succès${NC}"
else
    echo -e "${RED}   [ERREUR] Échec de l'application des migrations${NC}"
    cd "$SCRIPT_DIR"
    exit 1
fi

cd "$SCRIPT_DIR"

# Résumé
echo -e "\n${CYAN}"
echo "========================================"
echo "  SETUP TERMINÉ AVEC SUCCÈS"
echo "========================================"
echo -e "${NC}"

echo -e "${NC}[INFO] Base de données PostgreSQL prête:${NC}"
echo -e "${GRAY}   Host: localhost:5432${NC}"
echo -e "${GRAY}   Database: weatherforecastdb${NC}"
echo -e "${GRAY}   User: weatheruser${NC}"

echo -e "\n${YELLOW}[INFO] Vous pouvez maintenant lancer les applications:${NC}"
echo -e "${CYAN}   Terminal 1: cd application && dotnet run${NC}"
echo -e "${CYAN}   Terminal 2: cd api && dotnet run${NC}"

echo -e "\n${GREEN}[OK] Application et API partageront la même base PostgreSQL${NC}\n"
