// Notifications génériques pour toute l'application
// Utilisation: showNotification("Titre", "Message", "success|info|warning|danger")

export function showNotification(title, message, type = "info") {
    // Normaliser les paramètres: autoriser showNotification(title, type)
    const knownTypes = new Set(["primary","secondary","success","danger","warning","info","light","dark"]);
    if (typeof type === "undefined" && knownTypes.has(String(message))) {
        type = String(message);
        message = "";
    }
    const notification = document.createElement("div");
    notification.className = `alert alert-${type} alert-dismissible fade show app-notification`;
    notification.setAttribute("role", "alert");
    const time = new Date().toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });

    const contentWrap = document.createElement("div");
    contentWrap.className = "d-flex flex-column";

    const titleEl = document.createElement("div");
    titleEl.className = "fw-bold mb-1";
    titleEl.textContent = title;
    contentWrap.appendChild(titleEl);

    const msgEl = document.createElement("div");
    msgEl.textContent = message || "";
    contentWrap.appendChild(msgEl);

    const timeWrap = document.createElement("div");
    timeWrap.className = "text-end mt-2";
    const small = document.createElement("small");
    small.className = "text-muted";
    small.textContent = time;
    timeWrap.appendChild(small);
    contentWrap.appendChild(timeWrap);

    const closeBtn = document.createElement("button");
    closeBtn.type = "button";
    closeBtn.className = "btn-close";
    closeBtn.setAttribute("data-bs-dismiss", "alert");
    closeBtn.setAttribute("aria-label", "Close");

    notification.appendChild(contentWrap);
    notification.appendChild(closeBtn);

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

// Boîte de confirmation non bloquante (renvoie une Promise<boolean>)
export function confirmNotification(title, message = "", okText = "OK", cancelText = "Annuler") {
    return new Promise((resolve) => {
        const container = document.getElementById("notifications");
        if (!container) {
            // Fallback sur confirm si conteneur absent
            resolve(window.confirm(`${title}${message ? "\n\n" + message : ""}`));
            return;
        }

        // Créer le contenu
        const wrapper = document.createElement("div");
        wrapper.className = "alert alert-warning alert-dismissible fade show app-notification";
        wrapper.setAttribute("role", "alert");

        const content = document.createElement("div");
        content.className = "d-flex flex-column";

        const titleEl = document.createElement("div");
        titleEl.className = "fw-bold mb-1";
        titleEl.textContent = title;
        content.appendChild(titleEl);

        if (message) {
            const msgEl = document.createElement("div");
            msgEl.textContent = message;
            content.appendChild(msgEl);
        }

        const actions = document.createElement("div");
        actions.className = "mt-3 d-flex gap-2 justify-content-end";

        const okBtn = document.createElement("button");
        okBtn.type = "button";
        okBtn.className = "btn btn-sm btn-danger";
        okBtn.textContent = okText;

        const cancelBtn = document.createElement("button");
        cancelBtn.type = "button";
        cancelBtn.className = "btn btn-sm btn-secondary";
        cancelBtn.textContent = cancelText;

        actions.appendChild(cancelBtn);
        actions.appendChild(okBtn);
        content.appendChild(actions);

        const closeBtn = document.createElement("button");
        closeBtn.type = "button";
        closeBtn.className = "btn-close";
        closeBtn.setAttribute("data-bs-dismiss", "alert");
        closeBtn.setAttribute("aria-label", "Close");

        wrapper.appendChild(content);
        wrapper.appendChild(closeBtn);

        // Initialiser le stack si besoin
        if (!container.dataset.stackInit) {
            container.style.display = 'flex';
            container.style.flexDirection = 'column';
            container.style.gap = '8px';
            container.dataset.stackInit = 'true';
        }

        // Gestion des actions
        const cleanup = (result) => {
            try {
                wrapper.classList.remove("show");
                setTimeout(() => wrapper.remove(), 150);
            } catch (_) { }
            resolve(result);
        };

        okBtn.addEventListener('click', () => cleanup(true));
        cancelBtn.addEventListener('click', () => cleanup(false));
        closeBtn.addEventListener('click', () => cleanup(false));

        // Ajouter au DOM
        container.appendChild(wrapper);
    });
}

if (typeof window !== "undefined" && !window.confirmNotification) {
    window.confirmNotification = confirmNotification;
}
