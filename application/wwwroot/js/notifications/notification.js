// Notifications génériques pour toute l'application
// Utilisation: showNotification("Titre", "Message", "success|info|warning|danger")

export function showNotification(title, message, type = "info") {
    const notification = document.createElement("div");
    notification.className = `alert alert-${type} alert-dismissible fade show app-notification`;
    notification.setAttribute("role", "alert");
    notification.innerHTML = `
        <strong>${title}</strong> ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    const container = document.getElementById("notifications");
    if (container) {
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
