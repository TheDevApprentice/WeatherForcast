// ============================================
// USERS HUB - NOTIFICATIONS UTILISATEUR EN TEMPS RÉEL
// ============================================
// Ce fichier gère la connexion SignalR pour les notifications côté utilisateur

import { showNotification } from "./notifications/notification.js";

// Créer la connexion au UsersHub
const usersConnection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/users")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ============================================
// ÉVÉNEMENTS SIGNALR
// ============================================

// Email générique envoyé au user
usersConnection.on("EmailSentToUser", (payload) => {
    const subject = payload && payload.subject ? payload.subject : "Un email vient de vous être envoyé.";
    showNotification("Email envoyé", subject, "info");
    // Une notification reçue: on peut nettoyer le pending si présent
    clearPendingEmail();
});

// Email de vérification envoyé
usersConnection.on("VerificationEmailSentToUser", (payload) => {
    showNotification("Vérification", "Email de vérification envoyé. Vérifiez votre boîte.", "success");
    clearPendingEmail();
});

// ============================================
// OUTILS
// ============================================
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
    } catch (err) {
        console.warn("UsersHub: impossible de rejoindre le canal email:", err);
    }
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
        await joinEmailGroupIfPossible();
    } catch (err) {
        console.error("❌ Erreur de connexion UsersHub:", err);
        setTimeout(startUsersConnection, 3000);
    }
}

usersConnection.onreconnected(async () => {
    await joinEmailGroupIfPossible();
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
