#!/bin/bash
# ========================================
# Script d'Application des Migrations
# Pour Dev, Staging et Production
# ========================================

set -e  # Exit on error

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Paramètres
ENVIRONMENT="${1:-Development}"
DB_HOST="${2:-localhost}"
DB_PORT="${3:-5432}"
DB_NAME="${4:-weatherforecastdb}"
DB_USER="${5:-weatheruser}"
DB_PASSWORD="${6:-weatherpass}"

echo -e "${CYAN}"
echo "========================================"
echo "  APPLICATION DES MIGRATIONS"
echo "========================================"
echo -e "${NC}"

echo -e "${YELLOW}Environnement : $ENVIRONMENT${NC}"
echo -e "${YELLOW}Base de données : $DB_HOST:$DB_PORT/$DB_NAME${NC}"
echo ""

# Déterminer le chemin du script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR/../infra"

# Vérifier que PostgreSQL est accessible
echo -e "${YELLOW}[1/3] Vérification de la connexion à PostgreSQL...${NC}"
if ! pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" >/dev/null 2>&1; then
    echo -e "${RED}⚠️  PostgreSQL non accessible, tentative de connexion quand même...${NC}"
fi

# Construire la connection string
CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"

# Vérifier le build
echo -e "${YELLOW}[2/3] Vérification du build...${NC}"
if dotnet build -c Release --nologo >/dev/null 2>&1; then
    echo -e "${GREEN}   ✅ Build réussi${NC}"
else
    echo -e "${RED}   ❌ Erreur de build${NC}"
    exit 1
fi

# Appliquer les migrations
echo -e "${YELLOW}[3/3] Application des migrations...${NC}"
if dotnet ef database update --no-build; then
    echo -e "${GREEN}   ✅ Migrations appliquées avec succès${NC}"
else
    echo -e "${RED}   ❌ Échec de l'application des migrations${NC}"
    cd "$SCRIPT_DIR"
    exit 1
fi

cd "$SCRIPT_DIR"

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Migrations appliquées avec succès !${NC}"
echo -e "${GREEN}========================================${NC}\n"
