// ============================================
// ADMIN HUB - NOTIFICATIONS EN TEMPS RÉEL
// ============================================
// Ce fichier gère la connexion SignalR pour les notifications admin
// Seuls les utilisateurs avec le rôle Admin peuvent se connecter

// Importer showNotification (nécessite <script type="module">)
import { showNotification } from "./notifications/notification.js";

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
    // showNotification("Nouvel utilisateur", `${data.email} s'est enregistré`, "success");
    
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
    // showNotification("Connexion", `${data.email} s'est connecté`, "info");
    
    // Mettre à jour la dernière connexion dans la liste des users
    updateUserLastLogin(data.userId, data.loggedInAt);
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les sessions
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        // Rafraîchir avec un petit délai + retry, pour s'assurer que la session est bien persistée
        setTimeout(() => refreshUserSessions(data.userId), 400);
        setTimeout(() => refreshUserSessions(data.userId), 1500);
    }
});

// Événement : Utilisateur déconnecté
adminConnection.on("UserLoggedOut", (data) => {
    console.log("🚪 Utilisateur déconnecté:", data);
    // showNotification("Déconnexion", `${data.email} s'est déconnecté`, "info");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les sessions
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserSessions(data.userId);
    }
});

// Événement : Nouvelle session créée
adminConnection.on("SessionCreated", (data) => {
    console.log("📱 Nouvelle session créée:", data);
    
    // Si on est sur la page de détail de cet utilisateur, rafraîchir la liste via AJAX
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        // Laisser un délai pour que la DB soit à jour + un retry
        setTimeout(() => refreshUserSessions(data.userId), 600);
        setTimeout(() => refreshUserSessions(data.userId), 2000);
        // showNotification("Nouvelle session", `${data.email} - ${data.ipAddress}`, "info");
    }
});

// Événement : API Key créée
adminConnection.on("ApiKeyCreated", (data) => {
    console.log("🔑 API Key créée:", data);
    // showNotification("Nouvelle API Key", `${data.email} - ${data.keyName}`, "success");
    
    // Si on est sur la page de détail de cet utilisateur, mettre à jour les API keys
    const currentUserId = getCurrentUserIdFromPage();
    if (currentUserId === data.userId) {
        refreshUserApiKeys(data.userId);
    }
});

// Événement : API Key révoquée
adminConnection.on("ApiKeyRevoked", (data) => {
    console.log("🚫 API Key révoquée:", data);
    // showNotification("API Key révoquée", `${data.email} - ${data.keyName}`, "warning");
    
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
    // showNotification("Rôle modifié", `${data.email} - Rôle ${data.roleName} ${action}`, "info");
    
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
    // showNotification("Claim modifié", `${data.email} - ${data.claimType}=${data.claimValue} ${action}`, "info");
    
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

// Notifications: utiliser showNotification(title, message, type) depuis notifications/notification.js

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
    // Essayer /Admin/Details/{userId}
    let match = window.location.pathname.match(/\/Admin\/Details\/([^\/]+)/);
    if (match) return match[1];
    
    // Essayer window.currentUserId (défini dans Details.cshtml)
    if (typeof window.currentUserId !== 'undefined') {
        return window.currentUserId;
    }
    
    return null;
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
    console.log("🔄 Rafraîchissement des sessions pour user:", userId);
    const sessionsContainer = document.getElementById("user-sessions");
    if (!sessionsContainer) {
        console.warn("Container user-sessions introuvable");
        return;
    }
    
    fetch(`/Admin/GetUserSessions?userId=${userId}`)
        .then(response => response.json())
        .then(sessions => {
            console.log(`✅ ${sessions.length} sessions récupérées`);
            updateSessionsTable(sessions);
        })
        .catch(error => {
            console.error("❌ Erreur lors du chargement des sessions:", error);
            // Fallback: recharger la page
            location.reload();
        });
}

// Ajouter une session (désormais: délègue au rafraîchissement AJAX pour conserver le layout du tableau)
function addSessionToList(sessionData) {
    console.warn("addSessionToList() appelé - délégation au refresh AJAX (aucune insertion de carte)", sessionData);
    const userId = getCurrentUserIdFromPage();
    if (!userId) return;
    setTimeout(() => refreshUserSessions(userId), 200);
}

// Mettre à jour la dernière connexion d'un utilisateur dans la liste
function updateUserLastLogin(userId, loggedInAt) {
    console.log(`📅 Mise à jour dernière connexion pour user ${userId}:`, loggedInAt);
    
    // Chercher la ligne de l'utilisateur dans la table
    const userRows = document.querySelectorAll('tbody tr');
    console.log(`Nombre de lignes trouvées: ${userRows.length}`);
    
    let found = false;
    userRows.forEach((row, index) => {
        const detailsLink = row.querySelector('a[href*="/Admin/Details/"]');
        if (detailsLink) {
            console.log(`Ligne ${index}: ${detailsLink.href}`);
            if (detailsLink.href.includes(userId)) {
                found = true;
                console.log(`✅ Ligne trouvée pour user ${userId}`);
                
                // Trouver la colonne "Dernière connexion" (index peut varier)
                const cells = row.cells;
                console.log(`Nombre de cellules: ${cells.length}`);
                
                // Chercher la cellule qui contient "Jamais" ou une date
                for (let i = 0; i < cells.length; i++) {
                    const cellText = cells[i].textContent.trim();
                    if (cellText === 'Jamais' || cellText.match(/\d{2}\/\d{2}\/\d{4}/)) {
                        console.log(`📍 Cellule "Dernière connexion" trouvée à l'index ${i}`);
                        const date = new Date(loggedInAt);
                        cells[i].textContent = date.toLocaleString('fr-FR');
                        
                        // Ajouter un effet de surbrillance
                        cells[i].classList.add('bg-warning', 'bg-opacity-25');
                        setTimeout(() => {
                            cells[i].classList.remove('bg-warning', 'bg-opacity-25');
                        }, 2000);
                        break;
                    }
                }
            }
        }
    });
    
    if (!found) {
        console.warn(`❌ Utilisateur ${userId} non trouvé dans la liste`);
    }
}

// Rafraîchir les API keys d'un utilisateur
function refreshUserApiKeys(userId) {
    console.log("🔄 Rafraîchissement des API keys pour user:", userId);
    const apiKeysContainer = document.getElementById("user-apikeys");
    if (!apiKeysContainer) {
        console.warn("Container user-apikeys introuvable");
        return;
    }
    
    fetch(`/Admin/GetUserApiKeys?userId=${userId}`)
        .then(response => response.json())
        .then(apiKeys => {
            console.log(`✅ ${apiKeys.length} API keys récupérées`);
            updateApiKeysTable(apiKeys);
        })
        .catch(error => {
            console.error("❌ Erreur lors du chargement des API keys:", error);
            // Fallback: recharger la page
            location.reload();
        });
}

// Rafraîchir les rôles d'un utilisateur
function refreshUserRoles(userId) {
    console.log("Rafraîchissement des rôles pour user:", userId);
    // Recharger la page pour afficher les nouveaux rôles
    location.reload();
}

// Rafraîchir les claims d'un utilisateur
function refreshUserClaims(userId) {
    console.log("Rafraîchissement des claims pour user:", userId);
    // Recharger la page pour afficher les nouveaux claims
    location.reload();
}

// Mettre à jour la table des sessions
function updateSessionsTable(sessions) {
    const tbody = document.getElementById("user-sessions");
    if (!tbody) return;

    tbody.innerHTML = "";
    // Récupérer un éventuel anti-forgery token présent dans la page
    const antiForgeryInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const antiForgeryField = antiForgeryInput ? `<input type="hidden" name="__RequestVerificationToken" value="${antiForgeryInput.value}" />` : "";
    
    sessions.forEach(session => {
        const row = document.createElement("tr");
        row.className = "session-item-new"; // Effet de surbrillance
        
        // Colonne 1 : Type
        const td1 = document.createElement("td");
        const typeIcon = session.type === "Web" ? "🌐 Web" : "📱 Mobile";
        const typeBadge = session.type === "Web" ? "bg-primary" : "bg-info";
        td1.innerHTML = `<span class="badge ${typeBadge}">${typeIcon}</span>`;
        row.appendChild(td1);
        
        // Colonne 2 : IP
        const td2 = document.createElement("td");
        td2.innerHTML = `<small>${session.ipAddress || 'N/A'}</small>`;
        row.appendChild(td2);
        
        // Colonne 3 : User Agent
        const td3 = document.createElement("td");
        td3.innerHTML = `<small>${session.userAgent || 'N/A'}</small>`;
        row.appendChild(td3);
        
        // Colonne 4 : Statut
        const td4 = document.createElement("td");
        if (session.isRevoked) {
            td4.innerHTML = '<span class="badge bg-danger">🔴 Révoquée</span>';
        } else if (session.isExpired) {
            td4.innerHTML = '<span class="badge bg-warning">⏰ Expirée</span>';
        } else {
            td4.innerHTML = '<span class="badge bg-success">🟢 Active</span>';
        }
        row.appendChild(td4);
        
        // Colonne 5 : Expiration
        const td5 = document.createElement("td");
        const expiresAt = new Date(session.expiresAt);
        const day = String(expiresAt.getDate()).padStart(2, '0');
        const month = String(expiresAt.getMonth() + 1).padStart(2, '0');
        const year = expiresAt.getFullYear();
        const hours = String(expiresAt.getHours()).padStart(2, '0');
        const minutes = String(expiresAt.getMinutes()).padStart(2, '0');
        td5.innerHTML = `<small>${day}/${month}/${year} ${hours}:${minutes}</small>`;
        row.appendChild(td5);
        
        // Colonne 6 : Actions
        const td6 = document.createElement("td");
        if (session.isActive) {
            td6.innerHTML = `
                <form action="/Admin/RevokeSession/${session.id}" method="post" class="d-inline">
                    ${antiForgeryField}
                    <input type="hidden" name="userId" value="${window.currentUserId || ''}" />
                    <button type="submit" class="btn btn-sm btn-danger" 
                            onclick="return confirm('Révoquer cette session ?');">
                        <i class="bi bi-x-circle"></i> Révoquer
                    </button>
                </form>
            `;
        }
        row.appendChild(td6);
        
        tbody.appendChild(row);
        
        // Retirer l'effet après 3 secondes
        setTimeout(() => {
            row.classList.remove("session-item-new");
        }, 3000);
    });
    
    // Mettre à jour le compteur de sessions
    updateSessionsCount(sessions.filter(s => s.isActive).length);
}

// Mettre à jour la table des API Keys
function updateApiKeysTable(apiKeys) {
    const tbody = document.getElementById("user-apikeys");
    if (!tbody) return;

    tbody.innerHTML = "";
    
    apiKeys.forEach(apiKey => {
        const row = document.createElement("tr");
        row.className = "apikey-item-new"; // Effet de surbrillance
        
        let statusBadge;
        if (apiKey.isRevoked) {
            statusBadge = '<span class="badge bg-danger">🔴 Révoquée</span>';
        } else if (apiKey.isExpired) {
            statusBadge = '<span class="badge bg-warning">⏰ Expirée</span>';
        } else {
            statusBadge = '<span class="badge bg-success">🟢 Active</span>';
        }
        
        const lastUsed = apiKey.lastUsedAt 
            ? new Date(apiKey.lastUsedAt).toLocaleString('fr-FR')
            : 'Jamais';
        
        row.innerHTML = `
            <td><strong>${apiKey.name}</strong></td>
            <td><code>${apiKey.key}</code></td>
            <td><small>${apiKey.scopes}</small></td>
            <td>${statusBadge}</td>
            <td><small>${lastUsed}</small></td>
            <td><span class="badge bg-info">${apiKey.requestCount}</span></td>
        `;
        
        tbody.appendChild(row);
        
        // Retirer l'effet après 3 secondes
        setTimeout(() => {
            row.classList.remove("apikey-item-new");
        }, 3000);
    });
    
    // Mettre à jour le compteur d'API keys
    const activeCount = apiKeys.filter(k => !k.isRevoked && !k.isExpired).length;
    updateApiKeysCount(activeCount);
}

// Mettre à jour le compteur de sessions actives
function updateSessionsCount(count) {
    const badge = document.getElementById('sessions-count');
    if (badge) {
        console.log(`📊 Mise à jour compteur sessions: ${badge.textContent} → ${count}`);
        badge.textContent = count;
        // Effet de surbrillance
        badge.classList.remove('bg-info');
        badge.classList.add('bg-warning');
        setTimeout(() => {
            badge.classList.remove('bg-warning');
            badge.classList.add('bg-info');
        }, 1000);
    }
}

// Mettre à jour le compteur d'API keys actives
function updateApiKeysCount(count) {
    const badge = document.getElementById('apikeys-count');
    if (badge) {
        console.log(`📊 Mise à jour compteur API keys: ${badge.textContent} → ${count}`);
        badge.textContent = count;
        // Effet de surbrillance
        badge.classList.remove('bg-info');
        badge.classList.add('bg-warning');
        setTimeout(() => {
            badge.classList.remove('bg-warning');
            badge.classList.add('bg-info');
        }, 1000);
    }
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
