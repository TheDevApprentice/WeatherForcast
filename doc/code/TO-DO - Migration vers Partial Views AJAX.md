# 🎯 TO-DO - Migration vers Partial Views AJAX

## 🎨 Objectif Principal
**Remplacer le rendu HTML côté client par des Partial Views serveur + AJAX**

---

## 📋 Phase 1 - Analyse et Préparation

### 1.1 Identifier les blocs HTML générés côté client
- [ ] **Admin/Index.cshtml** : Table des utilisateurs (rendu via `renderUsers()`)
- [ ] **Admin/Details.cshtml** : Tables Sessions et API Keys (via `admin-realtime.js`)
- [ ] **WeatherForecast/Index.cshtml** : Cartes météo (via `weatherforecast-realtime.js`)
- [ ] **Autres pages** : Scanner pour d'éventuels autres blocs dynamiques

### 1.2 Créer la structure des Partial Views
- [ ] **Views/Admin/_UsersTable.cshtml** : Table des utilisateurs avec pagination
- [ ] **Views/Admin/_SessionsTable.cshtml** : Table des sessions utilisateur
- [ ] **Views/Admin/_ApiKeysTable.cshtml** : Table des clés API utilisateur
- [ ] **Views/WeatherForecast/_ForecastCards.cshtml** : Grille des cartes météo

---

## 🔧 Phase 2 - Implémentation Backend

### 2.1 AdminController - Endpoints AJAX
- [ ] **GetUsersPartial()** : Retourne la table des utilisateurs
  ```csharp
  [HttpGet]
  public async Task<IActionResult> GetUsersPartial(string search = "", string role = "", int page = 1)
  {
      var users = await _userService.SearchUsersAsync(search, role, page);
      return PartialView("_UsersTable", users);
  }
  ```

- [ ] **GetSessionsPartial()** : Retourne les sessions d'un utilisateur
  ```csharp
  [HttpGet]
  public async Task<IActionResult> GetSessionsPartial(string userId)
  {
      var sessions = await _sessionService.GetActiveSessionsAsync(userId);
      return PartialView("_SessionsTable", sessions);
  }
  ```

- [ ] **GetApiKeysPartial()** : Retourne les clés API d'un utilisateur
  ```csharp
  [HttpGet]
  public async Task<IActionResult> GetApiKeysPartial(string userId)
  {
      var apiKeys = await _apiKeyService.GetByUserIdAsync(userId);
      return PartialView("_ApiKeysTable", apiKeys);
  }
  ```

### 2.2 WeatherForecastController - Endpoints AJAX
- [ ] **GetForecastsPartial()** : Retourne les cartes météo
  ```csharp
  [HttpGet]
  public async Task<IActionResult> GetForecastsPartial()
  {
      var forecasts = await _weatherForecastService.GetAllAsync();
      return PartialView("_ForecastCards", forecasts);
  }
  ```

---

## 🎨 Phase 3 - Création des Partial Views

### 3.1 Admin/_UsersTable.cshtml
- [ ] **Structure** : Table Bootstrap avec colonnes (Email, Nom, Rôles, Statut, Actions)
- [ ] **Pagination** : Liens Previous/Next avec data-attributes pour AJAX
- [ ] **Actions** : Boutons Détails/Rôles avec confirmations via `confirmNotification`

### 3.2 Admin/_SessionsTable.cshtml
- [ ] **Structure** : Table des sessions avec colonnes (Type, IP, User Agent, Statut, Expiration, Actions)
- [ ] **Actions** : Bouton Révoquer avec confirmation
- [ ] **Animations** : Classes CSS pour nouveaux éléments

### 3.3 Admin/_ApiKeysTable.cshtml
- [ ] **Structure** : Table des clés API avec colonnes (Nom, Clé, Scopes, Statut, Dernière utilisation, Requêtes)
- [ ] **Animations** : Classes CSS pour nouveaux éléments

### 3.4 WeatherForecast/_ForecastCards.cshtml
- [ ] **Structure** : Grille de cartes Bootstrap avec données météo
- [ ] **Actions** : Boutons Détails/Modifier/Supprimer
- [ ] **Animations** : Classes CSS pour nouvelles cartes

---

## 🔄 Phase 4 - Migration JavaScript

### 4.1 Utilitaire AJAX centralisé
- [ ] **Créer `wwwroot/js/utils/ajax-helper.js`**
  ```javascript
  export async function loadPartial(url, containerId, showLoading = true) {
      if (showLoading) showLoadingSpinner(containerId);
      try {
          const response = await fetch(url);
          const html = await response.text();
          document.getElementById(containerId).innerHTML = html;
          return true;
      } catch (error) {
          showNotification('Erreur de chargement', error.message, 'danger');
          return false;
      } finally {
          if (showLoading) hideLoadingSpinner(containerId);
      }
  }
  ```

### 4.2 Migration Admin/Index.cshtml
- [ ] **Remplacer `renderUsers()`** par `loadPartial('/Admin/GetUsersPartial', 'userTableContainer')`
- [ ] **Supprimer la fonction `renderUsers()` et le template HTML**
- [ ] **Adapter les événements de recherche/pagination**

### 4.3 Migration admin-realtime.js
- [ ] **Remplacer `updateSessionsTable()`** par `loadPartial('/Admin/GetSessionsPartial?userId=...', 'user-sessions')`
- [ ] **Remplacer `updateApiKeysTable()`** par `loadPartial('/Admin/GetApiKeysPartial?userId=...', 'user-apikeys')`
- [ ] **Supprimer les fonctions de génération HTML**

### 4.4 Migration weatherforecast-realtime.js
- [ ] **Remplacer `addForecastRow()` et `updateForecastRow()`** par rechargement partiel
- [ ] **Adapter les événements SignalR** pour déclencher `loadPartial('/WeatherForecast/GetForecastsPartial', 'forecasts-container')`
- [ ] **Supprimer les fonctions de génération de cartes**

---

## ⚡ Phase 5 - Optimisations et Finitions

### 5.1 Loading States
- [ ] **Skeleton loaders** : Afficher pendant le chargement AJAX
- [ ] **Spinners** : Indicateurs visuels sur les boutons d'action
- [ ] **Transitions** : Animations fluides entre les états

### 5.2 Gestion d'erreurs
- [ ] **Retry automatique** : En cas d'échec réseau
- [ ] **Fallback** : Message d'erreur utilisateur-friendly
- [ ] **Logging** : Erreurs côté serveur dans les partials

### 5.3 Performance
- [ ] **Cache côté serveur** : Mise en cache des partials fréquents
- [ ] **Compression** : Gzip pour les réponses HTML
- [ ] **Lazy loading** : Chargement à la demande des sections non critiques

---

## 🎯 Bénéfices Attendus

### ✅ Avantages
- **Maintenabilité** : HTML géré côté serveur (Razor)
- **Sécurité** : Plus de génération HTML côté client
- **SEO/Accessibilité** : Rendu serveur natif
- **Cohérence** : Même moteur de template partout
- **Performance** : Moins de JavaScript à exécuter

### 📊 Métriques de Succès
- **Réduction JS** : -70% de code de génération HTML côté client
- **Temps de rendu** : Amélioration des Core Web Vitals
- **Maintenabilité** : Centralisation du HTML dans Razor
- **Sécurité** : Élimination des risques XSS côté client

---

## 🚀 Plan d'Exécution

### Sprint 1 (Admin Users Table)
1. Créer `AdminController.GetUsersPartial()`
2. Créer `Views/Admin/_UsersTable.cshtml`
3. Migrer `Admin/Index.cshtml` vers AJAX
4. Tester pagination et recherche

### Sprint 2 (Admin Sessions & API Keys)
1. Créer endpoints `GetSessionsPartial()` et `GetApiKeysPartial()`
2. Créer partials `_SessionsTable.cshtml` et `_ApiKeysTable.cshtml`
3. Migrer `admin-realtime.js`
4. Tester mises à jour temps réel

### Sprint 3 (WeatherForecast Cards)
1. Créer `WeatherForecastController.GetForecastsPartial()`
2. Créer `Views/WeatherForecast/_ForecastCards.cshtml`
3. Migrer `weatherforecast-realtime.js`
4. Tester ajout/modification/suppression temps réel

### Sprint 4 (Optimisations)
1. Implémenter loading states et gestion d'erreurs
2. Optimiser performance et caching
3. Tests d'intégration complets
4. Documentation technique