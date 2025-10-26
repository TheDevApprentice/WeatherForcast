# 🔐 Scripts de Génération de Certificat CI/CD

## 📋 Vue d'Ensemble

Ces scripts génèrent automatiquement un certificat X.509 pour Data Protection dans une pipeline CI/CD.

## 📂 Fichiers

| Fichier | Usage | Plateforme |
|---------|-------|------------|
| `generate-certificate.ps1` | Script PowerShell | Windows, Azure DevOps |
| `generate-certificate.sh` | Script Bash | Linux, GitHub Actions, GitLab CI |

---

## 🚀 Utilisation Locale

### **Windows (PowerShell)**

```powershell
# 1. Définir le mot de passe
$env:CERTIFICATE_PASSWORD = "VotreMotDePasseSecurise123!"

# 2. Exécuter le script
.\scripts\generate-certificate.ps1

# 3. Résultat
# certificates/
#   ├── weatherforecast-dataprotection.pfx
#   ├── weatherforecast-dataprotection.cer
#   ├── thumbprint.txt
#   └── .env.production
```

### **Linux / macOS (Bash)**

```bash
# 1. Rendre le script exécutable
chmod +x ./scripts/generate-certificate.sh

# 2. Définir le mot de passe
export CERTIFICATE_PASSWORD="VotreMotDePasseSecurise123!"

# 3. Exécuter le script
./scripts/generate-certificate.sh

# 4. Résultat
# certificates/
#   ├── weatherforecast-dataprotection.pfx
#   ├── weatherforecast-dataprotection.crt
#   ├── weatherforecast-dataprotection.key
#   ├── thumbprint.txt
#   └── .env.production
```

---

## 🔧 Paramètres

### **PowerShell**

```powershell
.\generate-certificate.ps1 `
    -OutputPath "./certs" `
    -CertificateName "my-app-cert" `
    -ValidityYears 10 `
    -CertificatePassword "SecurePass123"
```

| Paramètre | Description | Défaut |
|-----------|-------------|--------|
| `OutputPath` | Dossier de sortie | `./certificates` |
| `CertificateName` | Nom du fichier | `weatherforecast-dataprotection` |
| `ValidityYears` | Validité (années) | `5` |
| `CertificatePassword` | Mot de passe .pfx | Variable d'environnement |

### **Bash**

```bash
export OUTPUT_PATH="./certs"
export CERTIFICATE_NAME="my-app-cert"
export VALIDITY_DAYS=3650  # 10 ans
export CERTIFICATE_PASSWORD="SecurePass123"

./generate-certificate.sh
```

---

## 🔄 Intégration CI/CD

### **GitHub Actions**

Voir `.github/workflows/deploy-production.yml`

**Secrets requis** :
```
CERTIFICATE_PASSWORD          # Mot de passe du certificat
POSTGRES_PASSWORD             # Mot de passe PostgreSQL
JWT_SECRET                    # Secret JWT
SERVER_HOST                   # IP du serveur
SERVER_USER                   # User SSH
SERVER_SSH_KEY                # Clé privée SSH
```

**Usage** :
```yaml
- name: Generate Certificate
  run: |
    chmod +x ./scripts/generate-certificate.sh
    export CERTIFICATE_PASSWORD="${{ secrets.CERTIFICATE_PASSWORD }}"
    ./scripts/generate-certificate.sh
```

---

### **Azure DevOps**

Voir `azure-pipelines.yml`

**Variables requises** (dans groupe "production-secrets") :
```
CertificatePassword
PostgresPassword
JwtSecret
```

**Usage** :
```yaml
- task: PowerShell@2
  displayName: 'Generate Certificate'
  env:
    CERTIFICATE_PASSWORD: $(CertificatePassword)
  inputs:
    filePath: '$(Build.SourcesDirectory)/scripts/generate-certificate.ps1'
```

---

### **GitLab CI**

```yaml
generate-certificate:
  stage: setup
  image: alpine:latest
  before_script:
    - apk add --no-cache openssl bash
  script:
    - chmod +x ./scripts/generate-certificate.sh
    - export CERTIFICATE_PASSWORD="${CERTIFICATE_PASSWORD}"
    - ./scripts/generate-certificate.sh
  artifacts:
    paths:
      - certificates/
    expire_in: 1 week
  only:
    - main
```

---

## 🔐 Gestion des Secrets

### **Option 1 : Génération Unique**

1. Générer le certificat **une fois** localement
2. Encoder en base64 :
   ```bash
   # Linux/macOS
   base64 -w 0 certificates/weatherforecast-dataprotection.pfx > cert.b64
   
   # Windows
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("certificates/weatherforecast-dataprotection.pfx"))
   ```
3. Stocker dans les secrets CI/CD :
   - `CERTIFICATE_PFX_BASE64`
   - `CERTIFICATE_THUMBPRINT`
   - `CERTIFICATE_PASSWORD`

4. Restaurer dans la pipeline :
   ```bash
   echo "$CERTIFICATE_PFX_BASE64" | base64 -d > cert.pfx
   ```

---

### **Option 2 : Azure Key Vault**

```bash
# Upload
az keyvault secret set \
  --vault-name my-keyvault \
  --name dataprotection-cert-pfx \
  --file certificates/weatherforecast-dataprotection.pfx \
  --encoding base64

# Download dans la pipeline
az keyvault secret download \
  --vault-name my-keyvault \
  --name dataprotection-cert-pfx \
  --file cert.pfx \
  --encoding base64
```

---

### **Option 3 : HashiCorp Vault**

```bash
# Upload
vault kv put secret/dataprotection \
  cert=@certificates/weatherforecast-dataprotection.pfx \
  thumbprint="A1B2C3D4..."

# Download dans la pipeline
vault kv get -field=cert secret/dataprotection > cert.pfx
```

---

## 📊 Outputs du Script

### **Fichiers Générés**

| Fichier | Description | Usage |
|---------|-------------|-------|
| `.pfx` | Certificat + clé privée (PKCS#12) | Importé dans Docker |
| `.cer` / `.crt` | Certificat seul (clé publique) | Backup |
| `.key` | Clé privée (Bash uniquement) | Backup |
| `thumbprint.txt` | SHA-1 fingerprint | Config application |
| `.env.production` | Variables d'environnement | Docker Compose |

### **Variables Exportées**

Pour **GitHub Actions** :
```bash
CERTIFICATE_THUMBPRINT  # Disponible via ${{ steps.xxx.outputs.CERTIFICATE_THUMBPRINT }}
```

Pour **Azure DevOps** :
```yaml
CERTIFICATE_THUMBPRINT  # Disponible via $(CERTIFICATE_THUMBPRINT)
```

---

## ✅ Checklist Déploiement

### **Première Fois**

- [ ] Générer le certificat
- [ ] Sauvegarder le .pfx en lieu sûr (Key Vault)
- [ ] Noter le thumbprint
- [ ] Configurer les secrets CI/CD
- [ ] Tester le build
- [ ] Déployer en production
- [ ] Vérifier les logs : `[Production] Data Protection using certificate: ...`

### **Renouvellement**

- [ ] Générer nouveau certificat
- [ ] Backup de l'ancien
- [ ] Update thumbprint dans secrets
- [ ] Redéployer
- [ ] Vérifier que les anciennes sessions restent valides

---

## 🛠️ Dépannage

### **Erreur : "OpenSSL not found" (Bash)**
```bash
# Ubuntu/Debian
sudo apt-get install openssl

# Alpine (Docker)
apk add --no-cache openssl

# macOS
brew install openssl
```

### **Erreur : "CERTIFICATE_PASSWORD not set"**
```bash
# Définir la variable d'environnement
export CERTIFICATE_PASSWORD="YourPassword"

# Ou dans GitHub Actions
env:
  CERTIFICATE_PASSWORD: ${{ secrets.CERTIFICATE_PASSWORD }}
```

### **Erreur : "Permission denied"**
```bash
# Rendre le script exécutable
chmod +x ./scripts/generate-certificate.sh
```

### **Certificat non reconnu en production**
```bash
# Vérifier le thumbprint
cat certificates/thumbprint.txt

# Vérifier dans Docker
docker-compose exec web printenv | grep THUMBPRINT

# Vérifier les logs
docker-compose logs web | grep "Data Protection"
```

---

## 📚 Ressources

- [ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/)
- [X.509 Certificates](https://www.ssl.com/faqs/what-is-an-x-509-certificate/)
- [OpenSSL Documentation](https://www.openssl.org/docs/)
- [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/)

---

## 🔄 Rotation du Certificat

```bash
# 1. Générer nouveau certificat
./scripts/generate-certificate.sh

# 2. Copier dans le volume Docker
docker run --rm -v certificates:/certs -v $(pwd)/certificates:/source alpine \
  cp /source/weatherforecast-dataprotection.pfx /certs/weatherforecast-dataprotection-new.pfx

# 3. Update thumbprint dans .env
DATAPROTECTION_CERTIFICATE_THUMBPRINT=NEW_THUMBPRINT

# 4. Redémarrer
docker-compose restart web api

# Note : Les anciennes clés restent déchiffrables si l'ancien certificat est toujours présent
```

---

**✅ Scripts prêts pour la production !**
