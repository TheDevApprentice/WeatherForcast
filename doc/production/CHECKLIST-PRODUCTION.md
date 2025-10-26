# ✅ Checklist Production - WeatherForecast

## 🎯 Résumé : État Actuel

### ✅ Ce Qui Est Prêt

| Composant | Dev | Prod | Commentaire |
|-----------|-----|------|-------------|
| **PostgreSQL** | ✅ | ✅ | Volume persistant configuré |
| **Application Web** | ✅ | ✅ | Dockerfile + volumes |
| **API REST** | ✅ | ✅ | Dockerfile + volumes |
| **Data Protection** | ✅ | ⚠️ | Config adaptive (certificat requis en prod) |
| **Sessions** | ✅ | ✅ | Table Sessions en DB |
| **Rate Limiting** | ✅ | ✅ | Brute force protection |
| **JWT** | ✅ | ✅ | Authentification API |
| **Volumes Docker** | N/A | ✅ | web-keys, api-keys, certificates |

---

## 🛠️ Mode Développement (Actuel)

### **Comment Lancer**

```bash
# 1. Démarrer PostgreSQL uniquement
docker-compose up -d postgres

# 2. Lancer l'application Web (local)
cd application
dotnet run
# Console affiche : [Development] Data Protection keys stored in: ...

# 3. Lancer l'API (local)
cd ../api
dotnet run
# Console affiche : [API Development] Data Protection keys stored in: ...
```

### **Vérifications**

```bash
# Vérifier que les clés sont créées localement
ls application/keys/
ls api/keys/

# Tester l'application
# Web : https://localhost:7203
# API : https://localhost:7252/swagger
```

### **État des Clés**
- 📁 Stockées dans `application/keys/` et `api/keys/` (local)
- 🔓 **Non chiffrées** (acceptable en dev)
- ✅ Persistantes entre redémarrages

---

## 🚀 Mode Production

### **Prérequis AVANT de Déployer**

- [ ] **Certificat créé** (voir `SETUP-PRODUCTION-CERTIFICATE.md`)
- [ ] **Thumbprint du certificat** sauvegardé
- [ ] **Fichier .env** configuré avec variables production
- [ ] **Certificat .pfx** copié dans volume Docker
- [ ] **Backup de la base** effectué si mise à jour

---

### **Étapes de Déploiement**

#### **1. Générer le Certificat**

```powershell
# Windows PowerShell
$cert = New-SelfSignedCertificate `
    -Subject "CN=WeatherForecast Production" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(5)

# Sauvegarder le thumbprint
Write-Host "Thumbprint: $($cert.Thumbprint)"

# Exporter en .pfx
$password = ConvertTo-SecureString -String "MotDePasseSecurise!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert `
    -FilePath "weatherforecast-dataprotection.pfx" `
    -Password $password
```

#### **2. Copier le Certificat dans Docker**

```bash
# Créer le volume
docker volume create certificates

# Copier le certificat
docker run --rm -v certificates:/certs `
    -v ${PWD}:/source alpine `
    cp /source/weatherforecast-dataprotection.pfx /certs/

# Vérifier
docker run --rm -v certificates:/certs alpine ls -lh /certs
```

#### **3. Configurer les Variables d'Environnement**

Éditer `.env` :
```bash
ASPNETCORE_ENVIRONMENT=Production
DATAPROTECTION_CERTIFICATE_THUMBPRINT=A1B2C3D4E5F67890...
POSTGRES_PASSWORD=UnMotDePasseTresSecurise!
JWT_SECRET=UneCleSuperSecuriseeDeMinimum32Caracteres!
```

#### **4. Build et Déploiement**

```bash
# Build les images
docker-compose build

# Démarrer tous les services
docker-compose up -d

# Vérifier les logs
docker-compose logs -f web
docker-compose logs -f api
```

#### **5. Vérifications Post-Déploiement**

```bash
# 1. Vérifier que les services sont up
docker-compose ps

# 2. Vérifier les logs Data Protection
docker-compose logs web | grep "Data Protection"
# Devrait afficher : [Production] Data Protection using certificate: A1B2C3D4...

docker-compose logs api | grep "Data Protection"
# Devrait afficher : [API Production] Data Protection using certificate: A1B2C3D4...

# 3. Vérifier que les clés sont chiffrées
docker-compose exec web cat /app/keys/key-*.xml
# Devrait contenir <encryptedSecret> au lieu de <masterKey>

# 4. Vérifier la connexion PostgreSQL
docker-compose exec web dotnet ef database get-context --project ../infra

# 5. Tester les applications
# Web : http://localhost:8080
# API : http://localhost:7252/swagger
```

---

## ⚠️ Ce Qui Manque ENCORE (Optionnel)

### **Pour une Production Complète**

1. **HTTPS / SSL** :
   ```yaml
   # docker-compose.yml
   web:
     ports:
       - "443:443"
     environment:
       - ASPNETCORE_URLS=https://+:443
       - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certificates/ssl.pfx
       - ASPNETCORE_Kestrel__Certificates__Default__Password=${SSL_CERT_PASSWORD}
   ```

2. **Reverse Proxy (Nginx/Traefik)** :
   - Gestion SSL/TLS
   - Load balancing
   - Rate limiting supplémentaire

3. **Logging Centralisé** :
   - Seq, Elasticsearch, Azure App Insights
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

4. **Health Checks** :
   ```csharp
   builder.Services.AddHealthChecks()
       .AddDbContextCheck<AppDbContext>()
       .AddRedis("redis-connection");
   ```

5. **CI/CD Pipeline** :
   - GitHub Actions / Azure DevOps
   - Tests automatisés
   - Déploiement automatique

6. **Backup Automatique** :
   ```bash
   # Cron job pour backup PostgreSQL
   docker exec weatherforecast-db pg_dump -U weatheruser weatherforecastdb > backup.sql
   ```

7. **Monitoring** :
   - Prometheus + Grafana
   - Alertes (PagerDuty, Slack)

---

## 🔄 Workflow de Mise à Jour

### **Mise à Jour de l'Application**

```bash
# 1. Build nouvelle version
docker-compose build

# 2. Arrêter (volumes conservés)
docker-compose down

# 3. Redémarrer
docker-compose up -d

# ✅ Les clés sont conservées
# ✅ Les sessions utilisateurs restent actives
# ✅ La base de données est intacte
```

### **Rotation du Certificat**

```bash
# 1. Générer nouveau certificat

# 2. Copier dans le volume
docker run --rm -v certificates:/certs -v ${PWD}:/source alpine \
    cp /source/weatherforecast-dataprotection-new.pfx /certs/

# 3. Mettre à jour .env avec nouveau thumbprint

# 4. Redémarrer
docker-compose restart web api
```

---

## 📋 Checklist Finale

### **Développement** ✅
- [x] PostgreSQL dans Docker
- [x] Application Web tourne en local
- [x] API REST tourne en local
- [x] Clés Data Protection en local (non chiffrées)
- [x] Configuration adaptative Dev/Prod
- [x] Dockerfiles prêts

### **Production** ⚠️
- [ ] Certificat généré et exporté
- [ ] Thumbprint sauvegardé dans .env
- [ ] Certificat copié dans volume Docker
- [ ] Variables d'environnement production configurées
- [ ] Tests de build Docker réussis
- [ ] Déploiement effectué
- [ ] Vérifications post-déploiement OK
- [ ] Backup de sécurité créé

---

## 🆘 Dépannage

### **Erreur : "Certificate not found"**
```bash
# Vérifier le thumbprint
echo $DATAPROTECTION_CERTIFICATE_THUMBPRINT

# Vérifier le certificat dans le volume
docker run --rm -v certificates:/certs alpine ls -lh /certs
```

### **Erreur : "Unable to decrypt keys"**
```bash
# Le thumbprint a changé ou le certificat est manquant
# Solution : Réimporter le bon certificat
```

### **Clés non chiffrées en production**
```bash
# Vérifier l'environnement
docker-compose exec web printenv | grep ASPNETCORE_ENVIRONMENT
# Doit être : Production

# Vérifier que le thumbprint est défini
docker-compose exec web printenv | grep DATAPROTECTION
```

---

**✅ Pour DEV : Tout est prêt ! Lance juste `docker-compose up -d postgres` puis `dotnet run`**

**⚠️ Pour PROD : Génère le certificat, configure `.env`, puis `docker-compose up -d`**
