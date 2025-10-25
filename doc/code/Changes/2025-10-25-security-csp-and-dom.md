# Journal des changements — 25/10/2025

Ce document explique pour chaque modification le pourquoi (motivation) et le quoi (différence avant/après).

---

## 1) application/Program.cs — CSP avec nonce par requête

- Pourquoi
  - Durcir la CSP en supprimant `unsafe-inline` et réduire les risques XSS.
  - Permettre d’autoriser précisément les scripts via un `nonce` unique par requête.
- Changement
  - Génération d’un nonce par requête et exposition dans `HttpContext.Items["CspNonce"]`.
  - Ajout du nonce à la directive `script-src` dans l’en-tête CSP.
- Avant
  - CSP sans nonce; scripts inline ou non signés potentiellement autorisés.
- Après
  - Exécution limitée aux scripts externes autorisés et à ceux portant le nonce.

---

## 2) application/Views/Shared/_Layout.cshtml — Ajout du nonce aux <script>

- Pourquoi
  - Rendre les `<script>` compatibles avec la CSP basée sur nonce.
- Changement
  - Récupération du nonce depuis `Context.Items` et ajout de `nonce="@cspNonce"` à toutes les balises `<script>`.
- Avant
  - Aucun nonce sur les scripts, incompatible avec une CSP stricte.
- Après
  - Tous les scripts requis sont explicitement autorisés par nonce.

---

## 3) application/wwwroot/js/admin-realtime.js — Remplacement de innerHTML

- Pourquoi
  - Éviter l’injection HTML (XSS) et respecter la CSP.
- Changement
  - Ajout de helpers sûrs (`clearElement`, `el`).
  - Remplacement des usages `innerHTML` (tables Sessions/API Keys, formulaires) par `createElement`/`textContent`/`appendChild`.
  - Injection du token anti-forgery via `<input type="hidden">` au lieu de concaténation HTML.
- Avant
  - Plusieurs blocs construits au template string HTML.
- Après
  - Même rendu, construction DOM sûre et lisible.

---

## 4) application/wwwroot/js/notifications/notification.js — Notification sans innerHTML

- Pourquoi
  - Empêcher l’injection de HTML arbitraire dans les notifications.
- Changement
  - Remplacement du template `innerHTML` par création de nœuds (titre, message, horodatage, bouton close) via DOM API.
- Avant
  - Rendu par chaîne HTML interpolée.
- Après
  - Rendu équivalent, contenu textuel sécurisé.

---

## 5) application/wwwroot/js/utils/connection-status.js — Badge d’état sans innerHTML

- Pourquoi
  - Uniformiser la stratégie anti-XSS et respecter CSP.
- Changement
  - Remplacement de `el.innerHTML = ...` par suppression des enfants + `createElement('span')` + `textContent`.
- Avant
  - Insertion via `innerHTML`.
- Après
  - Construction DOM sûre et explicite.

---

## 6) application/wwwroot/js/weatherforecast-realtime.js — Cartes météo sans innerHTML

- Pourquoi
  - Supprimer deux gros templates HTML et réduire la surface XSS.
- Changement
  - Ajout de helpers `clearElement` et `el`.
  - Réécriture de `addForecastRow` et `updateForecastRow` en DOM programmatique.
- Avant
  - Gros templates injectés via `innerHTML`.
- Après
  - Même UI/UX, assemblage DOM sécurisé.

---

## 7) Tiers non modifiés (wwwroot/lib)

- Pourquoi
  - Conserver l’intégrité des bibliothèques tierces (jQuery, Bootstrap) pour faciliter les mises à jour.
- Changement
  - Aucun sur les fichiers tiers; seuls les scripts applicatifs ont été modifiés.
- Avant/Après
  - Identiques pour `lib/`.

---

## 8) Effet global attendu

- Sécurité renforcée (CSP stricte, pas d’`unsafe-inline`, nonce par requête).
- Réduction importante des surfaces XSS.
- Parité fonctionnelle conservée.
- Code client plus explicite et testable.

---

## 9) Mises à jour complémentaires (CSP, vues et UX) — itération suivante

- Pourquoi
  - Lever les dernières violations CSP constatées après les changements documentés plus haut (sur Admin/Auth/ApiKeys).
  - Garantir le chargement des modules et des styles sans `unsafe-inline`.
  - Améliorer l’UX (notifications et confirmation non bloquante).
- Changements
  - CSP (application/Program.cs)
    - Passage à des directives séparées: `style-src-elem 'self' 'nonce-{cspNonce}' https://cdn.jsdelivr.net;` et `style-src-attr 'unsafe-inline';`.
    - Ajout/maintien de `connect-src https://cdn.jsdelivr.net` (pour maps/libraries) et nonce déjà en place sur `script-src`.
  - Layout (Views/Shared/_Layout.cshtml)
    - Retrait du lien `~/web.styles.css` (MIME vide non servi) pour éviter les erreurs de type.
    - Retrait de styles inline résiduels (z-index…) pour conformité CSP.
  - WeatherForecast/Index.cshtml
    - `weatherforecast-realtime.js` chargé en `type="module"` et balises `<style>` avec `nonce` pour animations.
  - Admin (Index/Details/EditRoles/Create)
    - Tous les scripts SignalR balisés avec `nonce`.
    - `admin-realtime.js` chargé en `type="module"`.
    - Balises `<style>` internes (skeleton loader) balisées avec `nonce`.
    - Nettoyage d’un style inline remplacé par classe utilitaire Bootstrap.
  - ApiKeys/Index.cshtml
    - Suppression des handlers inline (`onclick`, `onsubmit`) remplacés par bindings JS dans un script balisé `nonce`.
    - Boutons “Copier” utilisent désormais `data-target` + JS (Clipboard API) et affichent une notification succès.
    - Formulaires de révocation: confirmation via notification personnalisée (au lieu de `confirm()` navigateur).
  - Notifications
    - `hubs-bootstrap.js` importe `./notifications/notification.js` pour exposer `window.showNotification` globalement.
    - Ajout de `confirmNotification(title, message?, okText?, cancelText?)` retournant `Promise<bool>` (UI Bootstrap dans `#notifications`, pas de styles inline), exposé en `window.confirmNotification`.
- Avant
  - Violations CSP sur styles/handlers inline, script module non déclaré, confirmations via alertes natives.
- Après
  - Pages conformes CSP (nonce, pas d’inline handlers/styles), UX améliorée (notifications et confirmation non bloquante), chargement ES modules correct.

---

## 10) Sanitation centralisée côté client (HtmlSanitizer)

- Pourquoi
  - Assurer un échappement cohérent pour toutes les valeurs textuelles injectées en HTML côté client.
  - Réduire le risque XSS si des templates HTML sont utilisés.
- Changements
  - `application/wwwroot/js/utils/html-sanitizer.js`
    - Ajout de `HtmlSanitizer.sanitize(input)` (échappement `& < > " '`).
    - Exposition globale via `window.HtmlSanitizer.sanitize` (importé par `hubs-bootstrap.js`).
  - `application/wwwroot/js/hubs-bootstrap.js`
    - Import de `./utils/html-sanitizer.js` pour rendre le sanitizer disponible partout.
  - `application/Views/Admin/Index.cshtml`
    - Remplacement de la fonction locale `escapeHtml` par `HtmlSanitizer.sanitize(...)` dans le rendu dynamique de la table.
- Avant
  - Échappement local ponctuel (fonction ad hoc) et dépendance implicite à `textContent`.
- Après
  - API d’échappement unique et réutilisable; code plus lisible et maintenable.
