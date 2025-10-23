// ============================================
// ADMIN HUB - NOTIFICATIONS EN TEMPS RÉEL
// ============================================
// Ce fichier gère la connexion SignalR pour les notifications admin
// Seuls les utilisateurs avec le rôle Admin peuvent se connecter

// Créer la connexion au AdminHub
const adminConnection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/admin")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ============================================
// ÉVÉNEMENTS SIGNALR
// ============================================

// Événement : Nouvel utilisateur enregistré
adminConnection.on("UserRegistered", (data) => {
    console.log("🆕 Nouvel utilisateur enregistré:", data);
    showAdminNotification("Nouvel utilisateur", `${data.email} s'est enregistré`, "success");
    
    // Mettre à jour la liste des users si on est sur la page users
    const isOnUsersPage = window.location.pathname === "/Admin" || 
                          window.location.pathname === "/Admin/" || 
                          window.location.pathname === "/Admin/Index";
    if (isOnUsersPage) {
        // Attendre un peu pour que la DB soit à jour
        setTimeout(() => refreshUsersList(), 500);
    }
});

// Événement : Utilisateur connecté
adminConnection.on("UserLoggedIn", (data) => {
    console.log("🔐 Utilisateur connecté:", data);
    showAdminNotification("Connexion", `${data.email} s'est connecté`, "info");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les sessions
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserSessions(data.userId);
    }
});

// Événement : Utilisateur déconnecté
adminConnection.on("UserLoggedOut", (data) => {
    console.log("🚪 Utilisateur déconnecté:", data);
    showAdminNotification("Déconnexion", `${data.email} s'est déconnecté`, "info");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les sessions
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserSessions(data.userId);
    }
});

// Événement : Nouvelle session créée
adminConnection.on("SessionCreated", (data) => {
    console.log("📱 Nouvelle session créée:", data);
    
    // Si on est sur la page de détail de cet utilisateur, ajouter la session en temps réel
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        addSessionToList(data);
        showAdminNotification("Nouvelle session", `${data.email} - ${data.ipAddress}`, "info");
    }
});

// Événement : API Key créée
adminConnection.on("ApiKeyCreated", (data) => {
    console.log("🔑 API Key créée:", data);
    showAdminNotification("Nouvelle API Key", `${data.email} - ${data.keyName}`, "success");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les API keys
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserApiKeys(data.userId);
    }
});

// Événement : API Key révoquée
adminConnection.on("ApiKeyRevoked", (data) => {
    console.log("🚫 API Key révoquée:", data);
    showAdminNotification("API Key révoquée", `${data.email} - ${data.keyName}`, "warning");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les API keys
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserApiKeys(data.userId);
    }
});

// Événement : Rôle utilisateur changé
adminConnection.on("UserRoleChanged", (data) => {
    console.log("👤 Rôle utilisateur changé:", data);
    const action = data.isAdded ? "ajouté" : "retiré";
    showAdminNotification("Rôle modifié", `${data.email} - Rôle ${data.roleName} ${action}`, "info");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les rôles
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserRoles(data.userId);
    }
});

// Événement : Claim utilisateur changé
adminConnection.on("UserClaimChanged", (data) => {
    console.log("🎫 Claim utilisateur changé:", data);
    const action = data.isAdded ? "ajouté" : "retiré";
    showAdminNotification("Claim modifié", `${data.email} - ${data.claimType}=${data.claimValue} ${action}`, "info");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les claims
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserClaims(data.userId);
    }
});

// ============================================
// GESTION DE LA CONNEXION
// ============================================

// Événement : Reconnexion en cours
adminConnection.onreconnecting((error) => {
    console.warn("⚠️ Reconnexion au AdminHub en cours...", error);
    updateAdminConnectionStatus("reconnecting");
});

// Événement : Reconnecté
adminConnection.onreconnected((connectionId) => {
    console.log("✅ Reconnecté au AdminHub:", connectionId);
    updateAdminConnectionStatus("connected");
});

// Événement : Connexion fermée
adminConnection.onclose((error) => {
    console.error("❌ Connexion AdminHub fermée:", error);
    updateAdminConnectionStatus("disconnected");
});

// Démarrer la connexion
async function startAdminConnection() {
    try {
        await adminConnection.start();
        console.log("✅ Connecté au AdminHub SignalR");
        updateAdminConnectionStatus("connected");
    } catch (err) {
        console.error("❌ Erreur de connexion AdminHub:", err);
        updateAdminConnectionStatus("disconnected");
        // Réessayer après 5 secondes
        setTimeout(startAdminConnection, 5000);
    }
}

// ============================================
// FONCTIONS UTILITAIRES
// ============================================

// Afficher une notification admin
function showAdminNotification(title, message, type = "info") {
    // Créer l'élément de notification
    const notification = document.createElement("div");
    notification.className = `alert alert-${type} alert-dismissible fade show admin-notification`;
    notification.setAttribute("role", "alert");
    notification.innerHTML = `
        <strong>${title}</strong> ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    // Ajouter au conteneur de notifications
    const container = document.getElementById("admin-notifications");
    if (container) {
        container.appendChild(notification);
        
        // Supprimer automatiquement après 5 secondes
        setTimeout(() => {
            notification.classList.remove("show");
            setTimeout(() => notification.remove(), 150);
        }, 5000);
    }
}

// Mettre à jour le statut de connexion
function updateAdminConnectionStatus(status) {
    const statusElement = document.getElementById("admin-connection-status");
    if (!statusElement) return;

    const statusConfig = {
        connected: { text: "Connecté", class: "bg-success", icon: "✓" },
        reconnecting: { text: "Reconnexion...", class: "bg-warning", icon: "⚠" },
        disconnected: { text: "Déconnecté", class: "bg-danger", icon: "✗" }
    };

    const config = statusConfig[status] || statusConfig.disconnected;
    statusElement.innerHTML = `<span class="badge ${config.class}">${config.icon} ${config.text}</span>`;
}

// Récupérer l'ID de l'utilisateur depuis l'URL (page de détail)
function getCurrentUserIdFromPage() {
    const match = window.location.pathname.match(/\/Admin\/Users\/Details\/([^\/]+)/);
    return match ? match[1] : null;
}

// Rafraîchir la liste des utilisateurs
function refreshUsersList() {
    console.log("Rafraîchissement de la liste des users...");
    
    // Si la fonction performSearch existe (page Index.cshtml), l'appeler
    if (typeof performSearch === 'function') {
        performSearch(true);
    } else {
        // Sinon, recharger la page
        location.reload();
    }
}

// Rafraîchir les sessions d'un utilisateur
function refreshUserSessions(userId) {
    console.log("Rafraîchissement des sessions pour user:", userId);
    // Implémenter le rechargement AJAX des sessions
    const sessionsContainer = document.getElementById("user-sessions");
    if (sessionsContainer) {
        fetch(`/Admin/Users/GetSessions/${userId}`)
            .then(response => response.json())
            .then(sessions => {
                // Mettre à jour l'UI avec les nouvelles sessions
                updateSessionsUI(sessions);
            })
            .catch(error => console.error("Erreur lors du chargement des sessions:", error));
    }
}

// Ajouter une session à la liste en temps réel
function addSessionToList(sessionData) {
    const sessionsContainer = document.getElementById("user-sessions");
    if (!sessionsContainer) return;

    const sessionElement = document.createElement("div");
    sessionElement.className = "list-group-item list-group-item-action session-item-new";
    sessionElement.innerHTML = `
        <div class="d-flex w-100 justify-content-between">
            <h6 class="mb-1">Session ${sessionData.sessionId.substring(0, 8)}...</h6>
            <small class="text-success">Nouvelle</small>
        </div>
        <p class="mb-1">
            <strong>IP:</strong> ${sessionData.ipAddress || "N/A"}<br>
            <strong>User Agent:</strong> ${sessionData.userAgent || "N/A"}
        </p>
        <small>Créée: ${new Date(sessionData.createdAt).toLocaleString()}</small>
    `;
    
    sessionsContainer.prepend(sessionElement);
    
    // Retirer l'effet "nouvelle" après 3 secondes
    setTimeout(() => {
        sessionElement.classList.remove("session-item-new");
    }, 3000);
}

// Rafraîchir les API keys d'un utilisateur
function refreshUserApiKeys(userId) {
    console.log("Rafraîchissement des API keys pour user:", userId);
    // Implémenter le rechargement AJAX des API keys
}

// Rafraîchir les rôles d'un utilisateur
function refreshUserRoles(userId) {
    console.log("Rafraîchissement des rôles pour user:", userId);
    // Implémenter le rechargement AJAX des rôles
}

// Rafraîchir les claims d'un utilisateur
function refreshUserClaims(userId) {
    console.log("Rafraîchissement des claims pour user:", userId);
    // Implémenter le rechargement AJAX des claims
}

// Mettre à jour l'UI des sessions
function updateSessionsUI(sessions) {
    const sessionsContainer = document.getElementById("user-sessions");
    if (!sessionsContainer) return;

    sessionsContainer.innerHTML = "";
    sessions.forEach(session => {
        const sessionElement = document.createElement("div");
        sessionElement.className = "list-group-item";
        sessionElement.innerHTML = `
            <div class="d-flex w-100 justify-content-between">
                <h6 class="mb-1">Session ${session.id.substring(0, 8)}...</h6>
                <small>${session.isActive ? '<span class="badge bg-success">Active</span>' : '<span class="badge bg-secondary">Expirée</span>'}</small>
            </div>
            <p class="mb-1">
                <strong>IP:</strong> ${session.ipAddress || "N/A"}<br>
                <strong>User Agent:</strong> ${session.userAgent || "N/A"}
            </p>
            <small>Créée: ${new Date(session.createdAt).toLocaleString()}</small>
        `;
        sessionsContainer.appendChild(sessionElement);
    });
}

// ============================================
// INITIALISATION
// ============================================

// Démarrer la connexion au chargement de la page
if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", startAdminConnection);
} else {
    startAdminConnection();
}
