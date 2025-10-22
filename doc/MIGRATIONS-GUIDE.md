# 🔄 Guide des Migrations de Base de Données

Guide complet pour gérer les migrations Entity Framework dans tous les environnements.

---

## 📋 Table des Matières

1. [Scripts Disponibles](#scripts-disponibles)
2. [Développement Local](#développement-local)
3. [Production](#production)
4. [Pipelines CI/CD](#pipelines-cicd)
5. [Cas d'Usage](#cas-dusage)
6. [Dépannage](#dépannage)

---

## 🛠️ Scripts Disponibles

| Script | Plateforme | Usage |
|--------|-----------|-------|
| `setup-database.ps1` | Windows | Setup initial complet (Docker + Migrations) |
| `setup-database.sh` | Linux/macOS | Setup initial complet (Docker + Migrations) |
| `apply-migrations.ps1` | Windows | Appliquer les migrations uniquement |
| `apply-migrations.sh` | Linux/macOS | Appliquer les migrations uniquement |

---

## 🛠️ Développement Local

### **Setup Initial (Première Fois)**

Utilise les scripts de setup complets qui :
1. Démarrent PostgreSQL dans Docker
2. Créent les migrations
3. Appliquent les migrations

```powershell
# Windows
.\scripts\setup-database.ps1
```

```bash
# Linux/macOS
chmod +x ./scripts/setup-database.sh
./scripts/setup-database.sh
```

**Ce que fait le script** :
- ✅ Vérifie Docker
- ✅ Démarre PostgreSQL (port 5432)
- ✅ Attend que PostgreSQL soit prêt
- ✅ Crée la migration initiale
- ✅ Applique la migration
- ✅ Prêt à lancer `dotnet run`

---

### **Ajouter une Nouvelle Migration (Après Modification du Modèle)**

Quand tu modifies une entité (ex: `domain/Entities/ApplicationUser.cs`) :

```bash
# 1. Aller dans le dossier infra
cd infra

# 2. Créer une migration
dotnet ef migrations add NomDeLaMigration

# 3. Appliquer la migration
dotnet ef database update
```

**Ou utiliser le script dédié** :

```powershell
# Windows
.\scripts\apply-migrations.ps1
```

```bash
# Linux/macOS
./scripts/apply-migrations.sh
```

---

## 🚀 Production

### **Premier Déploiement**

Les **pipelines CI/CD** appliquent automatiquement les migrations :

**GitHub Actions** :
```yaml
# .github/workflows/deploy-production.yml
# Étape automatique :
- Démarre PostgreSQL
- Attend qu'il soit prêt
- Applique les migrations
- Démarre Web + API
```

**Azure DevOps** :
```yaml
# azure-pipelines.yml
# Étape automatique :
- Démarre PostgreSQL
- Attend qu'il soit prêt
- Applique les migrations
- Démarre Web + API
```

---

### **Mise à Jour de l'Application**

Lors d'un `git push origin main` :

1. **Pipeline build** les nouvelles images Docker
2. **Pipeline applique** automatiquement les nouvelles migrations
3. **Pipeline redémarre** les containers

**Les migrations sont appliquées AVANT le redémarrage des services** pour éviter les erreurs.

---

### **Migration Manuelle en Production**

Si tu dois appliquer une migration manuellement :

```bash
# 1. Se connecter au serveur
ssh user@production-server

# 2. Aller dans le dossier
cd /opt/weatherforecast

# 3. Appliquer les migrations
docker-compose run --rm \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=weatherforecastdb;Username=weatheruser;Password=SECRET" \
  web dotnet ef database update --project /src/infra --startup-project /src/application
```

---

## 🔄 Pipelines CI/CD

### **Comment Ça Fonctionne**

#### **Workflow de Déploiement avec Migrations**

```
1. git push origin main
   ↓
2. Pipeline CI/CD démarre
   ↓
3. Build des images Docker
   ↓
4. Déploiement sur serveur :
   │
   ├─ 4a. Démarre PostgreSQL
   │      docker-compose up -d postgres
   │
   ├─ 4b. Attend PostgreSQL (health check)
   │      for i in {1..30}; do pg_isready; done
   │
   ├─ 4c. Applique les migrations
   │      docker-compose run web dotnet ef database update
   │      ✅ Succès → Continue
   │      ❌ Échec → Arrête le déploiement
   │
   └─ 4d. Démarre tous les services
          docker-compose up -d
   ↓
5. Health checks
   ↓
6. ✅ Déploiement réussi
```

---

### **GitHub Actions - Configuration**

La pipeline applique automatiquement les migrations :

```yaml
# .github/workflows/deploy-production.yml (déjà configuré)

# Démarrer PostgreSQL
docker-compose up -d postgres

# Attendre PostgreSQL
for i in {1..30}; do
  if docker exec weatherforecast-db pg_isready; then
    break
  fi
  sleep 2
done

# Appliquer migrations
docker-compose run --rm \
  -e ConnectionStrings__DefaultConnection="Host=postgres;..." \
  web dotnet ef database update --project /src/infra --startup-project /src/application

# Démarrer tous les services
docker-compose up -d
```

**Logs de la Pipeline** :
```
⏳ Attente de PostgreSQL...
Attente... (1/30)
Attente... (2/30)
✅ PostgreSQL prêt
🔄 Application des migrations...
Build started...
Done.
✅ Migrations appliquées avec succès
```

---

### **Azure DevOps - Configuration**

Identique à GitHub Actions, voir `azure-pipelines.yml`.

---

## 📚 Cas d'Usage

### **1. Ajouter une Nouvelle Propriété à une Entité**

```csharp
// domain/Entities/ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }  // ← Nouvelle propriété
    public string? LastName { get; set; }   // ← Nouvelle propriété
}
```

**Étapes** :
```bash
# 1. Créer la migration
cd infra
dotnet ef migrations add AddUserNames

# 2. Appliquer en dev
dotnet ef database update

# 3. Tester localement
cd ../application
dotnet run

# 4. Commit et push
git add .
git commit -m "feat: ajouter nom et prénom utilisateur"
git push origin main

# 5. La pipeline applique automatiquement en prod ✅
```

---

### **2. Premier Déploiement d'une Nouvelle Application**

```bash
# 1. Configurer les secrets CI/CD (voir SETUP-CICD-SECRETS.md)

# 2. Push le code
git push origin main

# 3. La pipeline :
#    - Génère le certificat
#    - Build les images
#    - Démarre PostgreSQL
#    - Crée la base de données
#    - Applique toutes les migrations
#    - Démarre les services

# 4. ✅ Application déployée avec base de données prête
```

---

### **3. Rollback d'une Migration**

Si une migration cause des problèmes :

```bash
# En dev (local)
cd infra
dotnet ef database update PreviousMigrationName

# En production (via SSH)
ssh user@production-server
cd /opt/weatherforecast
docker-compose run --rm web \
  dotnet ef database update PreviousMigrationName --project /src/infra --startup-project /src/application
```

---

## 🆘 Dépannage

### **Erreur : "No migrations configuration type was found"**

**Cause** : `DbContext` n'est pas trouvé

**Solution** :
```bash
# Spécifier le projet explicitement
dotnet ef migrations add MyMigration --project infra --startup-project application
```

---

### **Erreur : "Could not connect to the server"**

**Cause** : PostgreSQL pas démarré

**Solution** :
```bash
# Vérifier que PostgreSQL tourne
docker-compose ps

# Démarrer PostgreSQL
docker-compose up -d postgres

# Attendre qu'il soit prêt
docker-compose logs -f postgres
```

---

### **Erreur : "Table already exists"**

**Cause** : Base de données déjà créée mais migrations désynchronisées

**Solution** :
```bash
# Supprimer la base et recréer
docker-compose down -v
./scripts/setup-database.ps1  # ou .sh
```

---

### **Erreur en Production : "Migration failed in pipeline"**

**Logs de la pipeline** :
```
❌ Échec des migrations
Build failed with 1 error(s).
```

**Causes possibles** :
1. **Erreur de syntaxe** dans la migration
2. **Conflit de données** (contrainte violée)
3. **PostgreSQL pas accessible**

**Solution** :
```bash
# 1. Vérifier les logs détaillés dans la pipeline

# 2. Tester localement d'abord
./scripts/apply-migrations.sh

# 3. Si erreur de contrainte, ajuster la migration :
cd infra
dotnet ef migrations remove
# Modifier le modèle
dotnet ef migrations add FixedMigration

# 4. Re-push
git add .
git commit -m "fix: corriger migration"
git push origin main
```

---

### **Migration Bloquée en Production**

Si la migration prend trop de temps :

```bash
# 1. Se connecter au serveur
ssh user@production-server

# 2. Vérifier les processus PostgreSQL
docker exec weatherforecast-db psql -U weatheruser -d weatherforecastdb -c "SELECT * FROM pg_stat_activity;"

# 3. Vérifier les locks
docker exec weatherforecast-db psql -U weatheruser -d weatherforecastdb -c "SELECT * FROM pg_locks;"

# 4. Si nécessaire, annuler la migration en cours
# (Attention : peut corrompre la base si mal fait)
```

---

## ✅ Bonnes Pratiques

### **Toujours**
- ✅ Tester les migrations localement avant de push
- ✅ Créer des migrations avec des noms descriptifs (`AddUserNames`, pas `Migration1`)
- ✅ Vérifier les scripts de migration générés avant de commit
- ✅ Faire des backups avant les migrations en production

### **Ne Jamais**
- ❌ Modifier manuellement une migration déjà appliquée
- ❌ Supprimer des migrations déjà en production
- ❌ Appliquer des migrations directement sur la base sans passer par EF
- ❌ Commit les fichiers de base de données (*.db, *.db-wal)

---

## 📊 Récapitulatif

| Environnement | Comment Appliquer | Automatique ? |
|---------------|-------------------|---------------|
| **Dev (Local)** | `setup-database.ps1` ou `.sh` | ❌ Manuel |
| **Dev (Après modif)** | `dotnet ef database update` | ❌ Manuel |
| **Production (Deploy)** | Pipeline CI/CD | ✅ Automatique |
| **Production (Manuel)** | SSH + `docker-compose run` | ❌ Manuel |

---

## 🎯 Commandes Rapides

```bash
# Setup initial complet
./scripts/setup-database.sh

# Créer une migration
cd infra && dotnet ef migrations add MyMigration

# Appliquer les migrations
./scripts/apply-migrations.sh

# Voir l'historique des migrations
cd infra && dotnet ef migrations list

# Rollback à une migration précédente
cd infra && dotnet ef database update PreviousMigrationName

# Supprimer la dernière migration (si pas encore appliquée)
cd infra && dotnet ef migrations remove
```

---

**✅ Les migrations sont maintenant intégrées dans ton workflow Dev et Prod ! 🚀**
