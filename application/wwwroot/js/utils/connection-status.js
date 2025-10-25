// Utilitaire commun pour afficher l'état de connexion SignalR dans #connection-status
// Utilisation: import { updateConnectionStatus } from "../utils/connection-status.js";

export function updateConnectionStatus(status) {
    const el = document.getElementById("connection-status");
    if (!el) return;
    const map = {
        connected: { text: "Connecté", cls: "bg-success", icon: "✓" },
        reconnecting: { text: "Reconnexion...", cls: "bg-warning", icon: "⚠" },
        disconnected: { text: "Déconnecté", cls: "bg-danger", icon: "✗" },
        connecting: { text: "Connexion...", cls: "bg-secondary", icon: "⏳" }
    };
    const cfg = map[status] || map.disconnected;
    // Clear existing content
    while (el.firstChild) el.removeChild(el.firstChild);
    const span = document.createElement("span");
    span.className = `badge ${cfg.cls}`;
    span.textContent = `${cfg.icon} ${cfg.text}`;
    el.appendChild(span);
}
