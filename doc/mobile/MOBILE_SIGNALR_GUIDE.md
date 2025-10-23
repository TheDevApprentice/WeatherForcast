# 📱 Guide SignalR pour applications mobiles

## 🎯 Objectif

Ce guide explique comment connecter une application mobile (iOS/Android) au Hub SignalR pour recevoir les notifications en temps réel des prévisions météo.

---

## 🏗️ Architecture

```
[App Mobile]
    ↓ (1) Authentification
[API REST] → Retourne JWT token
    ↓ (2) Connexion SignalR avec JWT
[WeatherForecastHub] → Connexion établie
    ↓ (3) Notifications temps réel
[App Mobile] ← Reçoit les notifications
```

---

## 📋 Prérequis

### 1. **Authentification**
L'application mobile doit d'abord s'authentifier via l'API REST pour obtenir un **JWT token**.

### 2. **Librairie SignalR**
Installer la librairie SignalR pour votre plateforme :

**iOS (Swift)** :
```swift
// Package.swift ou CocoaPods
dependencies: [
    .package(url: "https://github.com/moozzyk/SignalR-Client-Swift", from: "0.9.0")
]
```

**Android (Kotlin)** :
```gradle
// build.gradle
dependencies {
    implementation 'com.microsoft.signalr:signalr:7.0.0'
}
```

---

## 🔐 Étape 1 : Authentification

### Endpoint
```http
POST https://api.example.com/api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password"
}
```

### Réponse
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "user": {
    "id": "123",
    "email": "user@example.com",
    "name": "John Doe"
  }
}
```

**⚠️ Important** : Stocker le token de manière sécurisée (Keychain iOS, EncryptedSharedPreferences Android).

---

## 🔌 Étape 2 : Connexion au Hub SignalR

### URL du Hub
```
https://api.example.com/hubs/weatherforecast?access_token={JWT_TOKEN}
```

**⚠️ Note** : Le JWT est passé dans la **query string** car SignalR ne peut pas envoyer de headers HTTP dans les WebSockets.

---

### Exemple iOS (Swift)

```swift
import SignalRClient

class WeatherNotificationService {
    private var connection: HubConnection?
    private let jwtToken: String
    
    init(jwtToken: String) {
        self.jwtToken = jwtToken
    }
    
    func connect() {
        // Construire l'URL avec le token
        let hubUrl = "https://api.example.com/hubs/weatherforecast?access_token=\(jwtToken)"
        
        // Créer la connexion
        connection = HubConnectionBuilder(url: URL(string: hubUrl)!)
            .withLogging(minLogLevel: .info)
            .withAutoReconnect()
            .build()
        
        // Écouter les événements
        setupEventHandlers()
        
        // Démarrer la connexion
        connection?.start()
    }
    
    private func setupEventHandlers() {
        // Événement : Nouvelle prévision créée
        connection?.on(method: "ForecastCreated", callback: { (forecast: WeatherForecast) in
            print("📢 Nouvelle prévision: \(forecast.summary) - \(forecast.temperatureC)°C")
            self.showNotification(title: "Nouvelle prévision", body: forecast.summary)
        })
        
        // Événement : Prévision mise à jour
        connection?.on(method: "ForecastUpdated", callback: { (forecast: WeatherForecast) in
            print("📢 Prévision mise à jour: \(forecast.summary)")
            self.showNotification(title: "Prévision mise à jour", body: forecast.summary)
        })
        
        // Événement : Prévision supprimée
        connection?.on(method: "ForecastDeleted", callback: { (id: Int) in
            print("📢 Prévision supprimée: ID \(id)")
            self.showNotification(title: "Prévision supprimée", body: "ID: \(id)")
        })
        
        // Gestion de la connexion
        connection?.onConnected = {
            print("✅ Connecté au Hub SignalR")
        }
        
        connection?.onDisconnected = { error in
            print("❌ Déconnecté du Hub SignalR: \(error?.localizedDescription ?? "Unknown")")
        }
        
        connection?.onReconnecting = { error in
            print("⚠️ Reconnexion en cours...")
        }
        
        connection?.onReconnected = { connectionId in
            print("✅ Reconnecté au Hub SignalR: \(connectionId ?? "Unknown")")
        }
    }
    
    private func showNotification(title: String, body: String) {
        // Afficher une notification locale
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = body
        content.sound = .default
        
        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )
        
        UNUserNotificationCenter.current().add(request)
    }
    
    func disconnect() {
        connection?.stop()
    }
}

// Modèle de données
struct WeatherForecast: Codable {
    let id: Int
    let date: String
    let temperatureC: Int
    let temperatureF: Int
    let summary: String
}
```

---

### Exemple Android (Kotlin)

```kotlin
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState

class WeatherNotificationService(private val jwtToken: String) {
    private var connection: HubConnection? = null
    
    fun connect() {
        // Construire l'URL avec le token
        val hubUrl = "https://api.example.com/hubs/weatherforecast?access_token=$jwtToken"
        
        // Créer la connexion
        connection = HubConnectionBuilder.create(hubUrl)
            .withAutomaticReconnect()
            .build()
        
        // Écouter les événements
        setupEventHandlers()
        
        // Démarrer la connexion
        connection?.start()?.blockingAwait()
    }
    
    private fun setupEventHandlers() {
        // Événement : Nouvelle prévision créée
        connection?.on("ForecastCreated", { forecast: WeatherForecast ->
            println("📢 Nouvelle prévision: ${forecast.summary} - ${forecast.temperatureC}°C")
            showNotification("Nouvelle prévision", forecast.summary)
        }, WeatherForecast::class.java)
        
        // Événement : Prévision mise à jour
        connection?.on("ForecastUpdated", { forecast: WeatherForecast ->
            println("📢 Prévision mise à jour: ${forecast.summary}")
            showNotification("Prévision mise à jour", forecast.summary)
        }, WeatherForecast::class.java)
        
        // Événement : Prévision supprimée
        connection?.on("ForecastDeleted", { id: Int ->
            println("📢 Prévision supprimée: ID $id")
            showNotification("Prévision supprimée", "ID: $id")
        }, Int::class.java)
        
        // Gestion de la connexion
        connection?.onClosed { error ->
            println("❌ Déconnecté du Hub SignalR: ${error?.message}")
        }
    }
    
    private fun showNotification(title: String, body: String) {
        // Afficher une notification Android
        val notification = NotificationCompat.Builder(context, CHANNEL_ID)
            .setContentTitle(title)
            .setContentText(body)
            .setSmallIcon(R.drawable.ic_notification)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .build()
        
        notificationManager.notify(notificationId++, notification)
    }
    
    fun disconnect() {
        connection?.stop()
    }
}

// Modèle de données
data class WeatherForecast(
    val id: Int,
    val date: String,
    val temperatureC: Int,
    val temperatureF: Int,
    val summary: String
)
```

---

## 📡 Événements SignalR disponibles

| Événement | Paramètres | Description |
|-----------|-----------|-------------|
| `ForecastCreated` | `WeatherForecast` | Nouvelle prévision créée |
| `ForecastUpdated` | `WeatherForecast` | Prévision mise à jour |
| `ForecastDeleted` | `int id` | Prévision supprimée |

---

## 🔒 Sécurité

### 1. **Authentification obligatoire**
- Le Hub nécessite un JWT valide
- Sans token, la connexion est refusée (401 Unauthorized)

### 2. **Token dans la query string**
- Le JWT est passé dans l'URL : `?access_token={token}`
- ⚠️ **Attention** : Ne pas logger l'URL complète (risque de fuite du token)

### 3. **HTTPS obligatoire**
- Toutes les connexions doivent utiliser HTTPS/WSS
- Le token est chiffré en transit

### 4. **Expiration du token**
- Gérer le renouvellement du token avant expiration
- Reconnecter avec le nouveau token

---

## 🧪 Test de connexion

### 1. **Tester l'authentification**
```bash
curl -X POST https://api.example.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'
```

### 2. **Tester la connexion SignalR**
Utiliser un outil comme **Postman** ou **SignalR Client** pour tester :
- URL : `wss://api.example.com/hubs/weatherforecast?access_token={TOKEN}`
- Vérifier que la connexion s'établit (200 OK)

### 3. **Tester les notifications**
- Créer une prévision depuis l'application Web ou l'API
- Vérifier que l'app mobile reçoit la notification

---

## ⚡ Gestion de la batterie

### Recommandations

1. **Déconnecter en arrière-plan**
```swift
// iOS
func applicationDidEnterBackground(_ application: UIApplication) {
    weatherService.disconnect()
}

func applicationWillEnterForeground(_ application: UIApplication) {
    weatherService.connect()
}
```

2. **Utiliser les Push Notifications pour l'arrière-plan**
- SignalR pour l'app au premier plan
- Firebase/APNs pour l'app en arrière-plan

3. **Reconnexion automatique**
- Utiliser `.withAutoReconnect()` (iOS/Android)
- Gérer les erreurs de reconnexion

---

## 🐛 Dépannage

### Erreur : 401 Unauthorized
**Cause** : JWT invalide ou expiré  
**Solution** : Vérifier que le token est valide et non expiré

### Erreur : Connection refused
**Cause** : URL incorrecte ou serveur indisponible  
**Solution** : Vérifier l'URL du Hub et que le serveur est démarré

### Pas de notifications reçues
**Cause** : Événements mal configurés  
**Solution** : Vérifier que les noms d'événements correspondent exactement (`ForecastCreated`, etc.)

### Déconnexions fréquentes
**Cause** : Problème réseau ou timeout  
**Solution** : Activer la reconnexion automatique

---

## 📚 Ressources

- [SignalR Client Swift](https://github.com/moozzyk/SignalR-Client-Swift)
- [SignalR Client Java/Android](https://github.com/SignalR/SignalR-Client-Java)
- [ASP.NET Core SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr)

---

## ✅ Checklist d'intégration

- [ ] Installer la librairie SignalR
- [ ] Implémenter l'authentification JWT
- [ ] Créer le service de connexion SignalR
- [ ] Configurer les event handlers
- [ ] Gérer la reconnexion automatique
- [ ] Implémenter les notifications locales
- [ ] Tester la connexion
- [ ] Tester la réception des événements
- [ ] Gérer la déconnexion en arrière-plan
- [ ] Ajouter la gestion d'erreurs
