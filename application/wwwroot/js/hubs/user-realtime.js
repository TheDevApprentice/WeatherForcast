import { showNotification } from "../notifications/notification.js";
import { updateConnectionStatus } from "../utils/connection-status.js";

// ============================================
// USERS HUB - NOTIFICATIONS UTILISATEUR EN TEMPS RÉEL
// ============================================
// Ce fichier gère la connexion SignalR pour les notifications côté utilisateur

// Créer la connexion au UsersHub
const usersConnection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/users")
    .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ============================================
// ÉVÉNEMENTS SIGNALR
// ============================================

// Email générique envoyé au user
usersConnection.on("EmailSentToUser", (payload) => {
    const cId = payload?.CorrelationId || payload?.correlationId;
    if (hasProcessedCorrelation(cId)) return;
    const subject = payload && (payload.Subject || payload.subject) ? (payload.Subject || payload.subject) : "Un email vient de vous être envoyé.";
    showNotification("Email envoyé", subject, "info");
    markProcessedCorrelation(cId);
    // Une notification reçue: on peut nettoyer le pending si présent
    clearPendingEmail();
});

// Email de vérification envoyé
usersConnection.on("VerificationEmailSentToUser", (payload) => {
    const cId = payload?.CorrelationId || payload?.correlationId;
    if (hasProcessedCorrelation(cId)) return;
    const msg = payload?.Message || payload?.message || "Email de vérification envoyé. Vérifiez votre boîte.";
    showNotification("Vérification", msg, "success");
    markProcessedCorrelation(cId);
    clearPendingEmail();
});

// Session révoquée par l'admin
usersConnection.on("SessionRevoked", (payload) => {
    console.warn("🚪 Session révoquée par l'administrateur:", payload);
    const message = payload?.Message || "Votre session a été révoquée par un administrateur.";
    showNotification("Session révoquée", message, "warning");
});

// Logout forcé
usersConnection.on("ForceLogout", (payload) => {
    console.warn("🚪 Logout forcé:", payload);
    const reason = payload?.Reason || "Session révoquée";
    const redirectUrl = payload?.RedirectUrl || "/Auth/Login";
    
    showNotification("Déconnexion forcée", reason, "danger");
    
    // Attendre un peu pour que l'utilisateur voie la notification
    setTimeout(() => {
        // Rediriger vers la page de login
        window.location.href = redirectUrl;
    }, 2000);
});

// ============================================
// OUTILS
// ============================================
function getSeenCorrelationIds() {
    try {
        const raw = sessionStorage.getItem("wf_seen_corrids");
        const arr = raw ? JSON.parse(raw) : [];
        return Array.isArray(arr) ? new Set(arr) : new Set();
    } catch (_) {
        return new Set();
    }
}

function saveSeenCorrelationIds(set) {
    try {
        // Conserver au plus 100 derniers IDs
        const arr = Array.from(set).slice(-100);
        sessionStorage.setItem("wf_seen_corrids", JSON.stringify(arr));
    } catch (_) {}
}

function hasProcessedCorrelation(cId) {
    if (!cId) return false;
    const set = getSeenCorrelationIds();
    return set.has(cId);
}

function markProcessedCorrelation(cId) {
    if (!cId) return;
    const set = getSeenCorrelationIds();
    set.add(cId);
    saveSeenCorrelationIds(set);
}
function getPendingEmail() {
    try {
        return sessionStorage.getItem("wf_pending_email");
    } catch (_) { return null; }
}

function clearPendingEmail() {
    try {
        sessionStorage.removeItem("wf_pending_email");
        sessionStorage.removeItem("wf_pending_reason");
    } catch (_) {}
}

function getUserEmailForChannel() {
    // 0) Priorité au pending email (ex: après redirect Register -> Login)
    const pending = getPendingEmail();
    if (pending && pending.length > 3) return pending;
    // 1) Variable globale éventuelle
    if (typeof window.userEmail === "string" && window.userEmail.length > 3) {
        return window.userEmail;
    }
    // 2) Champ Email sur la page (register/login)
    const candidates = [
        'input[name="Email"]',
        '#Email',
        'input[type="email"]',
        'input[name="email"]'
    ];
    for (const sel of candidates) {
        const el = document.querySelector(sel);
        if (el && el.value && el.value.length > 3) {
            return el.value;
        }
    }
    return null;
}

async function joinEmailGroupIfPossible() {
    const email = getUserEmailForChannel();
    if (!email) return;
    try {
        await usersConnection.invoke("JoinEmailChannel", email);
        console.log("UsersHub: rejoint le canal email:", email);
        await fetchAndDisplayPending(email);
    } catch (err) {
        console.warn("UsersHub: impossible de rejoindre le canal email:", err);
    }
}

async function joinUserGroupIfAuthenticated() {
    // Vérifier si l'utilisateur est connecté en cherchant son ID
    const userId = getUserIdFromPage();
    if (!userId) return;
    
    try {
        await usersConnection.invoke("JoinUserGroup", userId);
        console.log("UsersHub: rejoint le groupe utilisateur:", userId);
    } catch (err) {
        console.warn("UsersHub: impossible de rejoindre le groupe utilisateur:", err);
    }
}

function getUserIdFromPage() {
    // Chercher l'ID utilisateur dans différents endroits possibles
    if (typeof window.currentUserId === "string" && window.currentUserId.length > 0) {
        return window.currentUserId;
    }
    
    // Chercher dans les meta tags
    const metaUserId = document.querySelector('meta[name="user-id"]');
    if (metaUserId && metaUserId.content) {
        return metaUserId.content;
    }
    
    // Chercher dans les éléments data-user-id
    const userIdElement = document.querySelector('[data-user-id]');
    if (userIdElement && userIdElement.dataset.userId) {
        return userIdElement.dataset.userId;
    }
    
    return null;
}

async function leaveEmailGroupIfPossible() {
    const email = getUserEmailForChannel();
    if (!email) return;
    try {
        await usersConnection.invoke("LeaveEmailChannel", email);
        console.log("UsersHub: a quitté le canal email:", email);
    } catch (err) {
        console.warn("UsersHub: erreur lors du leave du canal email:", err);
    }
}

// ============================================
// CONNEXION
// ============================================
async function startUsersConnection() {
    try {
        await usersConnection.start();
        console.log("✅ Connecté au UsersHub SignalR");
        updateConnectionStatus("connected");
        await joinEmailGroupIfPossible();
        await joinUserGroupIfAuthenticated();
    } catch (err) {
        console.error("❌ Erreur de connexion UsersHub:", err);
        updateConnectionStatus("disconnected");
        setTimeout(startUsersConnection, 3000);
    }
}

usersConnection.onreconnected(async () => {
    const email = getUserEmailForChannel();
    await joinEmailGroupIfPossible();
    await joinUserGroupIfAuthenticated();
    if (email) {
        await fetchAndDisplayPending(email);
    }
    updateConnectionStatus("connected");
});

usersConnection.onreconnecting(() => {
    updateConnectionStatus("reconnecting");
});

usersConnection.onclose(() => {
    updateConnectionStatus("disconnected");
});

window.addEventListener("beforeunload", async () => {
    try {
        await leaveEmailGroupIfPossible();
        await usersConnection.stop();
    } catch (_) {}
});

// Démarrer au chargement
if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", startUsersConnection);
} else {
    startUsersConnection();
}

async function fetchAndDisplayPending(email) {
    try {
        const items = await usersConnection.invoke("FetchPendingMailNotifications", email);
        if (!Array.isArray(items)) return;
        for (const it of items) {
            const type = it?.type;
            const payloadJson = it?.payload;
            let payload;
            try { payload = payloadJson ? JSON.parse(payloadJson) : {}; } catch { payload = {}; }
            const cId = payload?.CorrelationId || payload?.correlationId;
            if (type === "VerificationEmailSentToUser") {
                if (!hasProcessedCorrelation(cId)) {
                    showNotification("Vérification", payload?.Message || payload?.message, "success");
                    markProcessedCorrelation(cId);
                }
            } else if (type === "EmailSentToUser") {
                if (!hasProcessedCorrelation(cId)) {
                    const subject = payload?.Subject || payload?.subject;
                    showNotification("Email envoyé", subject, "info");
                    markProcessedCorrelation(cId);
                }
            }
        }
        if (items.length) {
            clearPendingEmail();
        }
    } catch (err) {
        console.warn("UsersHub: FetchPending a échoué", err);
    }
}
