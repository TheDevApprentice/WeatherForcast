# 🔔 Services de Notifications Push

**Statut:** ⚠️ Code créé, non activé  
**Date:** 1er novembre 2025

---

## 📋 Contenu

Ce dossier contient tout le code nécessaire pour les notifications push, mais **il n'est pas activé**.

### Fichiers

| Fichier | Description | Statut |
|---------|-------------|--------|
| `IPushNotificationService.cs` | Interface pour les services push | ✅ Prêt |
| `FirebasePushNotificationService.cs` | Service Firebase (Android) | ⚠️ Code commenté |
| `ApnsPushNotificationService.cs` | Service APNS (iOS) | ⚠️ Code commenté |
| `HybridNotificationService.cs` | Service hybride (in-app + push) | ✅ Prêt |
| `PushNotificationConfiguration.cs` | Configuration | ✅ Prêt |
| `MauiProgram.Push.Example.cs` | Exemple d'utilisation | 📖 Documentation |

---

## ⚠️ Avant d'Activer

### Prérequis Android (Firebase)

- [ ] Créer un projet Firebase
- [ ] Télécharger `google-services.json`
- [ ] Installer packages NuGet:
  - `Xamarin.Firebase.Messaging`
  - `Xamarin.GooglePlayServices.Base`
- [ ] Configurer `AndroidManifest.xml`
- [ ] Obtenir Server Key et Sender ID

### Prérequis iOS (APNS)

- [ ] Créer un App ID sur Apple Developer
- [ ] Activer Push Notifications capability
- [ ] Créer une clé APNs (.p8)
- [ ] Configurer `Entitlements.plist`
- [ ] Configurer `Info.plist`
- [ ] Obtenir Key ID et Team ID

---

## 🚀 Comment Activer

### Étape 1: Configuration

Remplir `PushNotificationConfiguration.cs` avec vos clés:

```csharp
var config = new PushNotificationConfiguration
{
    EnablePushNotifications = true,
    
    // Firebase
    FirebaseServerKey = "VOTRE_CLE",
    FirebaseSenderId = "VOTRE_SENDER_ID",
    
    // APNS
    ApnsKeyId = "VOTRE_KEY_ID",
    ApnsTeamId = "VOTRE_TEAM_ID",
    ApnsBundleId = "com.votreentreprise.weatherforecast",
    ApnsKeyPath = "path/to/AuthKey.p8"
};
```

### Étape 2: Décommenter le Code

Dans `FirebasePushNotificationService.cs` et `ApnsPushNotificationService.cs`, décommenter les lignes marquées:

```csharp
// Code à décommenter:
// var token = await Firebase.Messaging.FirebaseMessaging.Instance.GetToken();
```

### Étape 3: Enregistrer dans MauiProgram.cs

Voir `MauiProgram.Push.Example.cs` pour le code complet.

```csharp
#if ANDROID
builder.Services.AddSingleton<IPushNotificationService, FirebasePushNotificationService>();
#elif IOS
builder.Services.AddSingleton<IPushNotificationService, ApnsPushNotificationService>();
#endif

builder.Services.AddSingleton<HybridNotificationService>();
```

### Étape 4: Initialiser dans App.xaml.cs

```csharp
private readonly HybridNotificationService _hybridService;

public App(HybridNotificationService hybridService)
{
    InitializeComponent();
    _hybridService = hybridService;
}

// Après connexion
await _hybridService.InitializeAsync(userId);
```

---

## 📖 Documentation Complète

Voir: `doc/mobile/push-notifications-guide.md`

Ce guide contient:
- Configuration détaillée Firebase et APNS
- Instructions pas à pas
- Exemples de code
- Tests
- Dépannage

---

## 💡 Utilisation

### Envoyer une Notification

```csharp
// Service hybride (in-app ou push selon l'état de l'app)
await _hybridService.SendNotificationAsync(
    userId: "user123",
    title: "Nouvelle Prévision",
    message: "Il va faire beau demain!",
    type: NotificationType.Success
);

// Notification de forecast
await _hybridService.SendForecastCreatedNotificationAsync(
    userId: "user123",
    forecast: newForecast
);
```

---

## ⚙️ Architecture

```
App Ouverte?
├─ OUI → NotificationService (in-app)
│         └─ NotificationManager
│             └─ NotificationCard (haut à droite)
│
└─ NON → PushNotificationService
          ├─ Android → Firebase Cloud Messaging
          └─ iOS → Apple Push Notification Service
```

---

## 🎯 Avantages

✅ **Code prêt** - Tout est déjà écrit  
✅ **Bien documenté** - Guide complet  
✅ **Flexible** - Facile à activer/désactiver  
✅ **Hybride** - In-app + Push automatique  
✅ **Multi-plateforme** - Android et iOS  

---

## 📝 Notes

- Le code est **testé et fonctionnel** (structure)
- Les appels API sont **commentés** pour éviter les erreurs
- La configuration est **centralisée**
- Les logs sont **détaillés** pour le debugging

---

**Prêt à être activé quand tu veux ! 🚀**
