// Notifications génériques pour toute l'application
// Utilisation: showNotification("Titre", "Message", "success|info|warning|danger")

export function showNotification(title, message, type = "info") {
    const notification = document.createElement("div");
    notification.className = `alert alert-${type} alert-dismissible fade show app-notification`;
    notification.setAttribute("role", "alert");
    const time = new Date().toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
    notification.innerHTML = `
        <div class="d-flex flex-column">
            <div class="fw-bold mb-1">${title}</div>
            <div>${message || ""}</div>
            <div class="text-end mt-2"><small class="text-muted">${time}</small></div>
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    const container = document.getElementById("notifications");
    if (container) {
        // Initialise le conteneur pour empiler joliment
        if (!container.dataset.stackInit) {
            container.style.display = 'flex';
            container.style.flexDirection = 'column';
            container.style.gap = '8px';
            container.dataset.stackInit = 'true';
        }

        container.appendChild(notification);

        setTimeout(() => {
            notification.classList.remove("show");
            setTimeout(() => notification.remove(), 150);
        }, 5000);
    }
}

// Fallback global si utilisé sans bundler/module
if (typeof window !== "undefined" && !window.showNotification) {
    window.showNotification = showNotification;
}
