#!/bin/bash
# ========================================
# Script de Génération de Certificat
# Pour Pipeline CI/CD (Linux/Bash)
# ========================================

set -e  # Exit on error

# Paramètres
OUTPUT_PATH="${OUTPUT_PATH:-./certificates}"
CERTIFICATE_NAME="${CERTIFICATE_NAME:-weatherforecast-dataprotection}"
VALIDITY_DAYS="${VALIDITY_DAYS:-1825}"  # 5 ans
CERTIFICATE_PASSWORD="${CERTIFICATE_PASSWORD}"

# Couleurs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}  Génération Certificat Data Protection${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# Vérifier que le mot de passe est fourni
if [ -z "$CERTIFICATE_PASSWORD" ]; then
    echo -e "${RED}❌ CERTIFICATE_PASSWORD environment variable is required!${NC}"
    echo "Usage: export CERTIFICATE_PASSWORD='YourSecurePassword'; ./generate-certificate.sh"
    exit 1
fi

# Vérifier qu'OpenSSL est installé
if ! command -v openssl &> /dev/null; then
    echo -e "${RED}❌ OpenSSL n'est pas installé!${NC}"
    echo "Installation : sudo apt-get install openssl"
    exit 1
fi

# Créer le dossier de sortie
if [ ! -d "$OUTPUT_PATH" ]; then
    mkdir -p "$OUTPUT_PATH"
    echo -e "${GREEN}✅ Dossier créé : $OUTPUT_PATH${NC}"
fi

# Générer la clé privée RSA 4096 bits
echo -e "${YELLOW}🔐 Génération de la clé privée RSA-4096...${NC}"
KEY_PATH="$OUTPUT_PATH/$CERTIFICATE_NAME.key"
openssl genrsa -out "$KEY_PATH" 4096 2>/dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Clé privée générée${NC}"
else
    echo -e "${RED}❌ Erreur lors de la génération de la clé${NC}"
    exit 1
fi

# Générer le certificat auto-signé
echo -e "${YELLOW}📜 Génération du certificat X.509...${NC}"
CRT_PATH="$OUTPUT_PATH/$CERTIFICATE_NAME.crt"

openssl req -x509 -new -nodes \
    -key "$KEY_PATH" \
    -sha256 \
    -days "$VALIDITY_DAYS" \
    -out "$CRT_PATH" \
    -subj "/CN=WeatherForecast DataProtection Production/O=WeatherForecast/C=FR" 2>/dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Certificat généré${NC}"
else
    echo -e "${RED}❌ Erreur lors de la génération du certificat${NC}"
    exit 1
fi

# Convertir en format PKCS#12 (.pfx)
echo -e "${YELLOW}💾 Conversion en format .pfx...${NC}"
PFX_PATH="$OUTPUT_PATH/$CERTIFICATE_NAME.pfx"

openssl pkcs12 -export \
    -out "$PFX_PATH" \
    -inkey "$KEY_PATH" \
    -in "$CRT_PATH" \
    -password "pass:$CERTIFICATE_PASSWORD" 2>/dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Fichier .pfx créé${NC}"
else
    echo -e "${RED}❌ Erreur lors de la conversion en .pfx${NC}"
    exit 1
fi

# Calculer le thumbprint (SHA-1 fingerprint)
echo -e "${YELLOW}🔑 Calcul du thumbprint...${NC}"
THUMBPRINT=$(openssl x509 -in "$CRT_PATH" -fingerprint -noout | sed 's/SHA1 Fingerprint=//g' | sed 's/://g')

if [ -z "$THUMBPRINT" ]; then
    echo -e "${RED}❌ Erreur lors du calcul du thumbprint${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Thumbprint calculé${NC}"

# Sauvegarder le thumbprint
THUMBPRINT_FILE="$OUTPUT_PATH/thumbprint.txt"
echo "$THUMBPRINT" > "$THUMBPRINT_FILE"
echo -e "${GREEN}✅ Thumbprint sauvegardé : $THUMBPRINT_FILE${NC}"

# Créer un fichier .env pour Docker
ENV_FILE="$OUTPUT_PATH/.env.production"
cat > "$ENV_FILE" <<EOF
# Généré automatiquement le $(date '+%Y-%m-%d %H:%M:%S')
DATAPROTECTION_CERTIFICATE_THUMBPRINT=$THUMBPRINT
CERTIFICATE_PASSWORD=$CERTIFICATE_PASSWORD
EOF
echo -e "${GREEN}✅ Fichier .env créé : $ENV_FILE${NC}"

# Afficher les détails du certificat
echo ""
echo -e "${CYAN}📋 Détails du Certificat :${NC}"
openssl x509 -in "$CRT_PATH" -noout -subject -dates -fingerprint | sed 's/^/   /'

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${GREEN}  ✅ Certificat Généré avec Succès${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "${CYAN}📂 Fichiers générés :${NC}"
echo -e "   - $KEY_PATH"
echo -e "   - $CRT_PATH"
echo -e "   - $PFX_PATH"
echo -e "   - $THUMBPRINT_FILE"
echo -e "   - $ENV_FILE"
echo ""
echo -e "${CYAN}🔑 Thumbprint (à utiliser dans la config) :${NC}"
echo -e "${YELLOW}   $THUMBPRINT${NC}"
echo ""
echo -e "${CYAN}📋 Prochaines Étapes :${NC}"
echo "   1. Copier le fichier .pfx dans le volume Docker certificates"
echo "   2. Ajouter le thumbprint dans .env : DATAPROTECTION_CERTIFICATE_THUMBPRINT=$THUMBPRINT"
echo "   3. Redémarrer les containers : docker-compose up -d"
echo ""
echo -e "${YELLOW}⚠️  IMPORTANT : Sauvegarder le fichier .pfx en lieu sûr (Azure Key Vault, HashiCorp Vault, etc.)${NC}"
echo ""

# Exporter les variables pour GitHub Actions
if [ "$GITHUB_ACTIONS" = "true" ]; then
    echo -e "${CYAN}🔧 Configuration GitHub Actions...${NC}"
    echo "CERTIFICATE_THUMBPRINT=$THUMBPRINT" >> $GITHUB_OUTPUT
    echo -e "${GREEN}✅ Variable CERTIFICATE_THUMBPRINT exportée${NC}"
fi

# Exporter les variables pour Azure DevOps
if [ "$TF_BUILD" = "True" ]; then
    echo -e "${CYAN}🔧 Configuration Azure DevOps...${NC}"
    echo "##vso[task.setvariable variable=CERTIFICATE_THUMBPRINT;isOutput=true]$THUMBPRINT"
    echo -e "${GREEN}✅ Variable CERTIFICATE_THUMBPRINT exportée${NC}"
fi

# Retourner le thumbprint
echo "$THUMBPRINT"
