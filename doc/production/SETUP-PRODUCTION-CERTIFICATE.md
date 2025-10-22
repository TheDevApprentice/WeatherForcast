# 🔐 Setup Certificat Production - Guide Complet

## 📋 Prérequis

- Accès au serveur de production
- Docker et Docker Compose installés
- PowerShell (Windows) ou OpenSSL (Linux)

---

## 🎯 Étape 1 : Générer le Certificat

### **Option A : Windows (PowerShell)**

```powershell
# 1. Créer le certificat auto-signé
$cert = New-SelfSignedCertificate `
    -Subject "CN=WeatherForecast Production DataProtection" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(5) `
    -FriendlyName "WeatherForecast DataProtection Prod"

# 2. Afficher le thumbprint (IMPORTANT : sauvegarder)
Write-Host "📋 THUMBPRINT : $($cert.Thumbprint)" -ForegroundColor Green
Write-Host "Copiez ce thumbprint dans appsettings.Production.json"

# 3. Exporter le certificat (avec clé privée)
$password = ConvertTo-SecureString -String "MotDePasseSecurise123!" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "weatherforecast-dataprotection.pfx" -Password $password

# 4. Exporter aussi sans clé privée (optionnel - backup)
Export-Certificate -Cert $cert -FilePath "weatherforecast-dataprotection.cer"

Write-Host "✅ Certificat exporté vers : weatherforecast-dataprotection.pfx"
```

### **Option B : Linux (OpenSSL)**

```bash
# 1. Générer la clé privée
openssl genrsa -out dataprotection.key 4096

# 2. Créer le certificat (valide 5 ans)
openssl req -x509 -new -nodes \
  -key dataprotection.key \
  -sha256 \
  -days 1825 \
  -out dataprotection.crt \
  -subj "/CN=WeatherForecast Production DataProtection"

# 3. Convertir en format PKCS#12 (.pfx)
openssl pkcs12 -export \
  -out weatherforecast-dataprotection.pfx \
  -inkey dataprotection.key \
  -in dataprotection.crt \
  -password pass:MotDePasseSecurise123!

# 4. Obtenir le thumbprint (SHA-1)
openssl x509 -in dataprotection.crt -fingerprint -noout | sed 's/://g' | sed 's/SHA1 Fingerprint=//'

echo "✅ Certificat exporté vers : weatherforecast-dataprotection.pfx"
```

---

## 🐳 Étape 2 : Copier le Certificat dans Docker

### **Méthode A : Docker Volume**

```bash
# 1. Créer le volume pour les certificats
docker volume create certificates

# 2. Copier le certificat dans le volume
docker run --rm -v certificates:/certs \
  -v $(pwd):/source alpine \
  cp /source/weatherforecast-dataprotection.pfx /certs/

# 3. Vérifier
docker run --rm -v certificates:/certs alpine ls -lh /certs
```

### **Méthode B : Docker Secret (Docker Swarm)**

```bash
# 1. Créer le secret
docker secret create dataprotection-cert weatherforecast-dataprotection.pfx

# 2. Vérifier
docker secret ls
```

---

## ⚙️ Étape 3 : Configurer l'Application

### **1. Mettre à jour `appsettings.Production.json`**

```bash
# Exemple de thumbprint obtenu
# A1B2C3D4E5F67890A1B2C3D4E5F67890A1B2C3D4
```

**application/appsettings.Production.json** :
```json
{
  "DataProtection": {
    "CertificateThumbprint": "A1B2C3D4E5F67890A1B2C3D4E5F67890A1B2C3D4"
  }
}
```

**api/appsettings.Production.json** :
```json
{
  "DataProtection": {
    "CertificateThumbprint": "A1B2C3D4E5F67890A1B2C3D4E5F67890A1B2C3D4"
  }
}
```

### **2. Mettre à jour `docker-compose.yml`**

```yaml
services:
  web:
    build:
      context: .
      dockerfile: application/Dockerfile
    volumes:
      - web-keys:/app/keys
      - certificates:/app/certificates  # ← Volume certificats
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DataProtection__CertificateThumbprint=A1B2C3D4E5F67890A1B2C3D4E5F67890A1B2C3D4
      - CERTIFICATE_PATH=/app/certificates/weatherforecast-dataprotection.pfx
      - CERTIFICATE_PASSWORD=MotDePasseSecurise123!

  api:
    build:
      context: .
      dockerfile: api/Dockerfile
    volumes:
      - api-keys:/app/keys
      - certificates:/app/certificates  # ← Volume certificats
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DataProtection__CertificateThumbprint=A1B2C3D4E5F67890A1B2C3D4E5F67890A1B2C3D4
      - CERTIFICATE_PATH=/app/certificates/weatherforecast-dataprotection.pfx
      - CERTIFICATE_PASSWORD=MotDePasseSecurise123!

volumes:
  certificates:
    driver: local
```

### **3. Créer un script d'import du certificat**

**Dockerfile** (ajouter avant ENTRYPOINT) :
```dockerfile
# Installer les outils pour gérer les certificats
RUN apt-get update && apt-get install -y ca-certificates

# Copier le script d'import
COPY import-certificate.sh /app/
RUN chmod +x /app/import-certificate.sh
```

**import-certificate.sh** :
```bash
#!/bin/bash
set -e

if [ -f "$CERTIFICATE_PATH" ]; then
    echo "🔐 Importing certificate from $CERTIFICATE_PATH"
    
    # Importer dans le store .NET
    dotnet dev-certs https --clean
    dotnet dev-certs https --import "$CERTIFICATE_PATH" --password "$CERTIFICATE_PASSWORD"
    
    echo "✅ Certificate imported successfully"
else
    echo "⚠️  Certificate not found at $CERTIFICATE_PATH"
    echo "   Application will run without certificate encryption"
fi

# Démarrer l'application
exec dotnet application.dll
```

---

## 🚀 Étape 4 : Déploiement

```bash
# 1. Build les images
docker-compose build

# 2. Démarrer en production
docker-compose up -d

# 3. Vérifier les logs
docker-compose logs web | grep "Data Protection"
# Devrait afficher :
# [Production] Data Protection using certificate: A1B2C3D4...

docker-compose logs api | grep "Data Protection"
# Devrait afficher :
# [API Production] Data Protection using certificate: A1B2C3D4...
```

---

## 🔍 Vérification

### **1. Vérifier que le certificat est chargé**

```bash
# Web
docker-compose exec web ls -la /app/keys/
# Devrait montrer : key-xxx.xml avec <encryptedSecret>

# API
docker-compose exec api ls -la /app/keys/
# Devrait montrer : key-xxx.xml avec <encryptedSecret>
```

### **2. Voir le contenu d'une clé (chiffrée)**

```bash
docker-compose exec web cat /app/keys/key-*.xml
```

**Devrait ressembler à** :
```xml
<?xml version="1.0" encoding="utf-8"?>
<key id="f12eb680-592f-48f8-9adc-09363097bb6c">
  <creationDate>2025-10-21T22:00:00Z</creationDate>
  <descriptor>
    <encryptedSecret decryptorType="CertificateXmlDecryptor">
      <encryptedKey>
        <!-- Clé chiffrée avec RSA-2048 -->
        <value>MIIBvAIBADANBgkqhkiG9w0BAQ...</value>
      </encryptedKey>
      <thumbprint>A1B2C3D4E5F67890A1B2C3D4...</thumbprint>
    </encryptedSecret>
  </descriptor>
</key>
```

✅ Si vous voyez `<encryptedSecret>` → Le certificat fonctionne !  
❌ Si vous voyez `<masterKey>` → Le certificat n'est pas chargé

---

## 🔄 Rotation du Certificat

### **Quand renouveler ?**
- Certificat auto-signé : tous les 1-2 ans
- Certificat CA : avant expiration

### **Comment renouveler ?**

```bash
# 1. Générer nouveau certificat
# (suivre Étape 1)

# 2. Copier dans le volume
docker run --rm -v certificates:/certs \
  -v $(pwd):/source alpine \
  cp /source/weatherforecast-dataprotection-new.pfx /certs/

# 3. Mettre à jour appsettings.Production.json
# avec le nouveau thumbprint

# 4. Redémarrer
docker-compose restart web api

# 5. Les anciennes clés restent déchiffrables
#    car le vieux certificat est toujours dans le store
```

---

## ⚠️ Sécurité

### **À FAIRE ✅**
- ✅ Sauvegarder le certificat .pfx dans un coffre-fort (Vault)
- ✅ Utiliser un mot de passe fort pour le .pfx
- ✅ Limiter l'accès au volume `certificates`
- ✅ Ne JAMAIS commit le .pfx dans Git
- ✅ Utiliser des variables d'environnement pour les secrets

### **À NE PAS FAIRE ❌**
- ❌ Hardcoder le mot de passe du certificat
- ❌ Partager le certificat en clair par email
- ❌ Utiliser le même certificat pour dev et prod
- ❌ Oublier de backuper le certificat

---

## 📋 Checklist Déploiement

- [ ] Certificat généré et exporté (.pfx)
- [ ] Thumbprint sauvegardé
- [ ] Certificat copié dans volume Docker
- [ ] `appsettings.Production.json` mis à jour
- [ ] Variables d'environnement configurées
- [ ] docker-compose.yml mis à jour
- [ ] Build et déploiement réussis
- [ ] Logs vérifient que le certificat est utilisé
- [ ] Fichier key-xxx.xml contient `<encryptedSecret>`
- [ ] Backup du certificat créé et stocké en sécurité

---

## 🆘 Dépannage

### **Erreur : "Certificate not found"**
```bash
# Vérifier que le certificat est dans le volume
docker run --rm -v certificates:/certs alpine ls -lh /certs

# Vérifier le thumbprint
echo $DataProtection__CertificateThumbprint
```

### **Erreur : "Unable to decrypt"**
```bash
# Vérifier le mot de passe
echo $CERTIFICATE_PASSWORD

# Réimporter manuellement
docker-compose exec web bash
dotnet dev-certs https --import /app/certificates/weatherforecast-dataprotection.pfx --password "..."
```

### **Clés non chiffrées en production**
```bash
# Vérifier l'environnement
docker-compose exec web printenv | grep ASPNETCORE_ENVIRONMENT
# Doit être : Production

# Vérifier les logs
docker-compose logs web | grep "Data Protection"
```

---

**🎉 Votre application est maintenant sécurisée avec un certificat en production ! 🔐**
