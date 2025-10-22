# 🔐 Configuration des Secrets CI/CD

Guide rapide pour configurer les secrets dans GitHub Actions et Azure DevOps.

---

## ✅ Checklist Complète

### **📦 Scripts et Pipelines**

- [x] ✅ Script PowerShell créé (`scripts/generate-certificate.ps1`)
- [x] ✅ Script Bash créé (`scripts/generate-certificate.sh`)
- [x] ✅ Pipeline GitHub Actions créée (`.github/workflows/deploy-production.yml`)
- [x] ✅ Pipeline Azure DevOps créée (`azure-pipelines.yml`)
- [x] ✅ Documentation complète (`scripts/README.md`, `SETUP-PRODUCTION-CERTIFICATE.md`)
- [x] ✅ .gitignore configuré

### **🔐 Configuration des Secrets (À Faire)**

- [ ] Configurer les secrets dans GitHub Actions OU Azure DevOps
- [ ] Générer un mot de passe pour le certificat
- [ ] Générer un mot de passe PostgreSQL sécurisé
- [ ] Générer une clé JWT secrète (32+ caractères)
- [ ] Configurer les accès SSH au serveur de production
- [ ] Tester la pipeline en staging (optionnel)
- [ ] Déployer en production

---

## 🎯 Secrets Requis

### **Liste des Secrets**

| Secret | Description | Exemple | Où le générer ? |
|--------|-------------|---------|-----------------|
| `CERTIFICATE_PASSWORD` | Mot de passe du certificat .pfx | `MySecureP@ssw0rd123!` | Générateur de mot de passe |
| `POSTGRES_PASSWORD` | Mot de passe PostgreSQL | `PgS3cur3P@ss!` | Générateur de mot de passe |
| `JWT_SECRET` | Clé secrète JWT (32+ caractères) | `YourSuperSecretJwtKeyWith32Chars!` | Générateur de mot de passe |
| `SERVER_HOST` | IP ou domaine du serveur | `192.168.1.100` ou `prod.example.com` | Configuration serveur |
| `SERVER_USER` | Utilisateur SSH | `deploy` ou `ubuntu` | Configuration serveur |
| `SERVER_SSH_KEY` | Clé privée SSH (format PEM) | `-----BEGIN RSA PRIVATE KEY-----\n...` | `ssh-keygen` |

### **Liste des Variables (Non-Sensibles)**

| Variable | Description | Exemple | Type |
|----------|-------------|---------|------|
| `PRODUCTION_WEB_URL` | URL publique de l'application Web | `https://weatherforecast.yourdomain.com` | Variable |
| `PRODUCTION_API_URL` | URL publique de l'API REST | `https://api.weatherforecast.yourdomain.com` | Variable |

### **Secrets Optionnels (Si Certificat Existant)**

| Secret | Description | Quand utiliser ? |
|--------|-------------|------------------|
| `CERTIFICATE_PFX_BASE64` | Certificat .pfx encodé en base64 | Si réutilisation d'un certificat existant |
| `CERTIFICATE_THUMBPRINT` | Thumbprint du certificat existant | Si réutilisation d'un certificat existant |

---

## 🔧 GitHub Actions - Configuration

### **1. Accéder aux Secrets et Variables**

```
GitHub Repository
  → Settings
    → Secrets and variables
      → Actions
```

**Onglet "Secrets"** :
- Pour les valeurs sensibles (mots de passe, clés)

**Onglet "Variables"** :
- Pour les valeurs non-sensibles (URLs, noms)

### **2. Ajouter les Secrets**

#### **CERTIFICATE_PASSWORD**
```
Name: CERTIFICATE_PASSWORD
Value: MySecureP@ssw0rd123!
```

#### **POSTGRES_PASSWORD**
```
Name: POSTGRES_PASSWORD
Value: PgS3cur3P@ss!
```

#### **JWT_SECRET**
```
Name: JWT_SECRET
Value: YourSuperSecretJwtKeyWith32CharsMinimum!
```

#### **SERVER_HOST**
```
Name: SERVER_HOST
Value: 192.168.1.100
```

#### **SERVER_USER**
```
Name: SERVER_USER
Value: deploy
```

#### **SERVER_SSH_KEY**
```
Name: SERVER_SSH_KEY
Value: -----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
(copier toute la clé privée)
...
-----END RSA PRIVATE KEY-----
```

**Comment obtenir la clé SSH** :
```bash
# Générer une nouvelle clé SSH
ssh-keygen -t rsa -b 4096 -f ~/.ssh/deploy_key -N ""

# Afficher la clé privée (à copier dans GitHub)
cat ~/.ssh/deploy_key

# Copier la clé publique sur le serveur
ssh-copy-id -i ~/.ssh/deploy_key.pub deploy@192.168.1.100
```

### **3. Ajouter les Variables (Non-Sensibles)**

Cliquer sur l'onglet **"Variables"** (à côté de "Secrets")

#### **PRODUCTION_WEB_URL**
```
Name: PRODUCTION_WEB_URL
Value: https://weatherforecast.com
```

#### **PRODUCTION_API_URL**
```
Name: PRODUCTION_API_URL
Value: https://api.weatherforecast.com
```

### **4. Vérifier**

```
GitHub Repository
  → Settings
    → Secrets and variables
      → Actions
```

**Onglet "Secrets"** :
- ✅ CERTIFICATE_PASSWORD
- ✅ POSTGRES_PASSWORD
- ✅ JWT_SECRET
- ✅ SERVER_HOST
- ✅ SERVER_USER
- ✅ SERVER_SSH_KEY

**Onglet "Variables"** :
- ✅ PRODUCTION_WEB_URL
- ✅ PRODUCTION_API_URL

---

## 🔧 Azure DevOps - Configuration

### **1. Créer un Groupe de Variables**

```
Azure DevOps Project
  → Pipelines
    → Library
      → + Variable group
        → Variable group name: production-secrets
```

### **2. Ajouter les Variables**

| Name | Value | Secret ? |
|------|-------|----------|
| `CertificatePassword` | `MySecureP@ssw0rd123!` | ✅ Oui |
| `PostgresPassword` | `PgS3cur3P@ss!` | ✅ Oui |
| `JwtSecret` | `YourSuperSecretJwtKeyWith32CharsMinimum!` | ✅ Oui |

**Pour chaque variable** :
1. Cliquer sur **+ Add**
2. Entrer le **Name** et la **Value**
3. ✅ Cocher **Keep this value secret**
4. Cliquer sur **OK**

### **3. Configurer le Service Connection (SSH)**

```
Azure DevOps Project
  → Project settings
    → Service connections
      → New service connection
        → SSH
```

**Paramètres** :
- **Connection name** : `Production Server`
- **Host** : `192.168.1.100`
- **Username** : `deploy`
- **Password or Private Key** : Sélectionner **Private Key**
- **Private Key** : Coller la clé privée SSH

### **4. Lier le Groupe de Variables à la Pipeline**

Dans `azure-pipelines.yml` :
```yaml
variables:
  - group: production-secrets  # ← Déjà présent
```

### **5. Vérifier**

```
Pipelines → Library → production-secrets
```

Variables présentes :
- ✅ CertificatePassword (🔒)
- ✅ PostgresPassword (🔒)
- ✅ JwtSecret (🔒)

```
Project settings → Service connections
```

- ✅ Production Server (SSH)

---

## 🛠️ Génération des Valeurs Secrètes

### **1. Mot de Passe Certificat**

**PowerShell** :
```powershell
# Générer un mot de passe sécurisé de 32 caractères
-join ((48..57) + (65..90) + (97..122) + (33..47) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

**Bash** :
```bash
# Générer un mot de passe sécurisé de 32 caractères
openssl rand -base64 32
```

### **2. Mot de Passe PostgreSQL**

Même commande que ci-dessus, ou utiliser un gestionnaire de mots de passe (1Password, LastPass, Bitwarden).

### **3. JWT Secret**

**PowerShell** :
```powershell
# Générer une clé JWT de 64 caractères
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})
```

**Bash** :
```bash
# Générer une clé JWT de 64 caractères
openssl rand -base64 64 | tr -d '\n'
```

### **4. Clé SSH**

```bash
# Générer une paire de clés SSH
ssh-keygen -t ed25519 -f ~/.ssh/deploy_weatherforecast -C "deploy@weatherforecast"

# Ou RSA si ed25519 non supporté
ssh-keygen -t rsa -b 4096 -f ~/.ssh/deploy_weatherforecast -C "deploy@weatherforecast"

# Afficher la clé privée (pour GitHub/Azure DevOps)
cat ~/.ssh/deploy_weatherforecast

# Afficher la clé publique (pour le serveur)
cat ~/.ssh/deploy_weatherforecast.pub

# Copier la clé publique sur le serveur
ssh-copy-id -i ~/.ssh/deploy_weatherforecast.pub user@server-ip
```

---

## 🧪 Tester les Secrets

### **GitHub Actions**

**Créer un workflow de test** :
```yaml
name: Test Secrets

on: workflow_dispatch

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - name: Check Secrets
        run: |
          echo "✅ CERTIFICATE_PASSWORD: ${CERTIFICATE_PASSWORD:0:5}***"
          echo "✅ POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:0:5}***"
          echo "✅ JWT_SECRET: ${JWT_SECRET:0:5}***"
          echo "✅ SERVER_HOST: $SERVER_HOST"
          echo "✅ SERVER_USER: $SERVER_USER"
          echo "✅ SERVER_SSH_KEY: ${SERVER_SSH_KEY:0:30}***"
        env:
          CERTIFICATE_PASSWORD: ${{ secrets.CERTIFICATE_PASSWORD }}
          POSTGRES_PASSWORD: ${{ secrets.POSTGRES_PASSWORD }}
          JWT_SECRET: ${{ secrets.JWT_SECRET }}
          SERVER_HOST: ${{ secrets.SERVER_HOST }}
          SERVER_USER: ${{ secrets.SERVER_USER }}
          SERVER_SSH_KEY: ${{ secrets.SERVER_SSH_KEY }}
```

**Lancer** :
```
Actions → Test Secrets → Run workflow
```

### **Azure DevOps**

**Créer une pipeline de test** :
```yaml
trigger: none

variables:
  - group: production-secrets

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: PowerShell@2
    displayName: 'Check Secrets'
    inputs:
      targetType: 'inline'
      script: |
        Write-Host "✅ CertificatePassword: $($env:CERTIFICATE_PASSWORD.Substring(0,5))***"
        Write-Host "✅ PostgresPassword: $($env:POSTGRES_PASSWORD.Substring(0,5))***"
        Write-Host "✅ JwtSecret: $($env:JWT_SECRET.Substring(0,5))***"
    env:
      CERTIFICATE_PASSWORD: $(CertificatePassword)
      POSTGRES_PASSWORD: $(PostgresPassword)
      JWT_SECRET: $(JwtSecret)
```

---

## 📋 Checklist Complète du Premier Déploiement

### **Étape 1 : Configuration des Secrets CI/CD** ⚙️

- [ ] **Configurer les secrets** dans GitHub Actions OU Azure DevOps :
  - [ ] `CERTIFICATE_PASSWORD` : Mot de passe du certificat (32+ caractères)
  - [ ] `POSTGRES_PASSWORD` : Mot de passe PostgreSQL (32+ caractères)
  - [ ] `JWT_SECRET` : Clé secrète JWT (64+ caractères)
  - [ ] `SERVER_HOST` : IP ou domaine du serveur (ex: `192.168.1.100`)
  - [ ] `SERVER_USER` : Utilisateur SSH (ex: `deploy`)
  - [ ] `SERVER_SSH_KEY` : Clé privée SSH complète

- [ ] **Configurer les variables** (non-sensibles) :
  - [ ] `PRODUCTION_WEB_URL` : URL publique Web (ex: `https://weatherforecast.com`)
  - [ ] `PRODUCTION_API_URL` : URL publique API (ex: `https://api.weatherforecast.com`)

### **Étape 2 : Tests Locaux** 🧪

- [ ] **Tester le script localement** :
  ```bash
  # PowerShell
  $env:CERTIFICATE_PASSWORD = "TestPassword123!"
  .\scripts\generate-certificate.ps1
  
  # Bash
  export CERTIFICATE_PASSWORD="TestPassword123!"
  ./scripts/generate-certificate.sh
  ```
- [ ] Vérifier que le certificat est généré dans `certificates/`
- [ ] Vérifier que `thumbprint.txt` contient le fingerprint

### **Étape 3 : Sécurité Git** 🔒

- [ ] **Vérifier que le .gitignore fonctionne** :
  ```bash
  # Créer un fichier test
  echo "test" > certificates/test.pfx
  
  # Vérifier qu'il est ignoré
  git status
  # Ne devrait PAS apparaître dans la liste
  
  # Nettoyer
  rm certificates/test.pfx
  ```
- [ ] Confirmer que `certificates/`, `.env`, et `*.pfx` sont ignorés
- [ ] Vérifier qu'aucun secret n'est committé : `git log --all --full-history -- "*password*" "*secret*"`

### **Étape 4 : Premier Déploiement** 🚀

- [ ] **Déclencher la pipeline** :
  - **GitHub Actions** : `Actions → Deploy to Production → Run workflow`
  - **Azure DevOps** : `Pipelines → Deploy Production → Run pipeline`

- [ ] **Surveiller le déploiement** :
  - [ ] Build réussi (images Docker créées)
  - [ ] Certificat généré ou restauré
  - [ ] Déploiement SSH réussi
  - [ ] Containers démarrés

### **Étape 5 : Vérifications Post-Déploiement** ✅

- [ ] **Vérifier les logs de la pipeline** :
  - [ ] Aucun secret en clair dans les logs
  - [ ] Certificat chargé avec succès

- [ ] **Vérifier les logs des containers** :
  ```bash
  # Sur le serveur
  docker-compose logs web | grep "Data Protection"
  docker-compose logs api | grep "Data Protection"
  ```
  - [ ] Devrait afficher : `[Production] Data Protection using certificate: A1B2C3D4...`
  - [ ] ⚠️ Si affiche `[WARNING] No certificate configured` → Certificat manquant !

- [ ] **Vérifier les services** :
  ```bash
  docker-compose ps
  # Tous les services doivent être "Up"
  ```

- [ ] **Health check** :
  ```bash
  curl -f http://SERVER_HOST:8080/health || echo "❌ Web KO"
  curl -f http://SERVER_HOST:7252/health || echo "❌ API KO"
  ```

- [ ] **Tester l'application** :
  - [ ] Page d'accueil accessible
  - [ ] Inscription d'un utilisateur fonctionne
  - [ ] Connexion fonctionne
  - [ ] Cookie de session créé
  - [ ] Déconnexion fonctionne

### **Étape 6 : Validation Sécurité** 🔐

- [ ] **Vérifier le chiffrement des clés** :
  ```bash
  # Sur le serveur
  docker-compose exec web cat /app/keys/key-*.xml
  ```
  - [ ] Contient `<encryptedSecret>` (✅ Chiffré)
  - [ ] ⚠️ Si contient `<masterKey>` → Certificat non utilisé !

- [ ] **Backup des secrets** :
  - [ ] Secrets sauvegardés dans un gestionnaire de mots de passe
  - [ ] Certificat .pfx backupé en lieu sûr
  - [ ] Thumbprint documenté

### **Étape 7 : Documentation** 📝

- [ ] **Documenter le déploiement** :
  - [ ] URL de production notée
  - [ ] Date du déploiement
  - [ ] Version déployée (commit hash)
  - [ ] Thumbprint du certificat
  - [ ] Contact personne ayant accès aux secrets

---

## ✅ Checklist de Validation Rapide

### **Avant le Premier Déploiement**

- [ ] Tous les secrets sont configurés dans GitHub Actions OU Azure DevOps
- [ ] Les mots de passe sont forts (32+ caractères, mix majuscules/minuscules/chiffres/symboles)
- [ ] La clé SSH fonctionne (test : `ssh -i ~/.ssh/deploy_key user@server`)
- [ ] Le serveur est accessible depuis la pipeline CI/CD
- [ ] Les secrets sont marqués comme **secret** (masqués dans les logs)
- [ ] Backup des secrets effectué dans un gestionnaire de mots de passe
- [ ] Test de la pipeline en mode staging (optionnel)

### **Après Configuration**

- [ ] Lancer la pipeline manuellement pour tester
- [ ] Vérifier les logs : aucun secret en clair
- [ ] Vérifier le déploiement : `docker-compose ps`
- [ ] Vérifier Data Protection : `[Production] Data Protection using certificate: ...`
- [ ] Health check : Application accessible

---

## 🔐 Bonnes Pratiques de Sécurité

### **✅ À Faire**

- ✅ Utiliser des mots de passe forts (32+ caractères)
- ✅ Marquer tous les secrets comme "secret" dans CI/CD
- ✅ Sauvegarder les secrets dans un gestionnaire (1Password, LastPass, Azure Key Vault)
- ✅ Utiliser des clés SSH dédiées (une par environnement)
- ✅ Limiter les permissions SSH (user `deploy` non-root avec sudo limité)
- ✅ Activer 2FA sur GitHub/Azure DevOps
- ✅ Restreindre l'accès aux secrets (team leads uniquement)
- ✅ Auditer les accès aux secrets régulièrement
- ✅ Rotation des secrets tous les 6-12 mois

### **❌ À NE PAS Faire**

- ❌ Hardcoder les secrets dans le code
- ❌ Committer les secrets dans Git
- ❌ Partager les secrets par email/Slack
- ❌ Utiliser les mêmes secrets dev/prod
- ❌ Réutiliser les mêmes mots de passe
- ❌ Laisser les secrets en clair dans les logs
- ❌ Donner accès root via SSH
- ❌ Utiliser des mots de passe faibles

---

## 🆘 Dépannage

### **Erreur : Secret not found**

**GitHub Actions** :
```bash
# Vérifier que le secret existe
Settings → Secrets and variables → Actions
```

**Azure DevOps** :
```bash
# Vérifier que le groupe de variables est lié
azure-pipelines.yml → variables: - group: production-secrets
```

### **Erreur : Permission denied (SSH)**

```bash
# Tester la connexion SSH manuellement
ssh -i ~/.ssh/deploy_key user@server-ip

# Vérifier les permissions de la clé
chmod 600 ~/.ssh/deploy_key

# Vérifier que la clé publique est sur le serveur
cat ~/.ssh/authorized_keys  # Sur le serveur
```

### **Erreur : Certificate password incorrect**

```bash
# Vérifier le mot de passe localement
openssl pkcs12 -in cert.pfx -noout -password pass:YourPassword

# Si erreur : le mot de passe est incorrect
```

### **Secret visible dans les logs**

**GitHub Actions** :
```yaml
# Masquer automatiquement
echo "::add-mask::$SECRET_VALUE"
```

**Azure DevOps** :
- Variables marquées comme "secret" sont automatiquement masquées

---

## 📚 Ressources

- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure DevOps Variable Groups](https://learn.microsoft.com/en-us/azure/devops/pipelines/library/variable-groups)
- [SSH Key Generation](https://www.ssh.com/academy/ssh/keygen)
- [Password Security Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

---

## 🎉 Prêt à Déployer !

Une fois tous les secrets configurés :

```bash
# GitHub Actions
git push origin main
# → La pipeline démarre automatiquement

# Azure DevOps
git push origin main
# → La pipeline démarre automatiquement

# Ou déclencher manuellement
GitHub: Actions → Deploy to Production → Run workflow
Azure DevOps: Pipelines → Deploy Production → Run pipeline
```

**Vérifier le déploiement** :
```bash
# Logs de la pipeline
# Puis sur le serveur :
docker-compose ps
docker-compose logs web | grep "Data Protection"
# Devrait afficher : [Production] Data Protection using certificate: A1B2C3D4...
```

---

**✅ Configuration des secrets terminée ! Vous êtes prêt pour la production ! 🚀**
