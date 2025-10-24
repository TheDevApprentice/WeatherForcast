// ============================================
// SIGNALR - NOTIFICATIONS EN TEMPS RÉEL
// ============================================

// Importer showNotification (nécessite <script type="module">)
import { showNotification } from "./notifications/notification.js";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/weatherforecast")
    .withAutomaticReconnect([0, 1000, 3000, 5000, 10000]) // Retry strategy
    .configureLogging(signalR.LogLevel.Information)
    .build();

// ============================================
// ÉVÉNEMENTS SIGNALR
// ============================================

// Prévision créée
connection.on("ForecastCreated", (forecast) => {
    console.log("📢 Nouvelle prévision créée:", forecast);
    
    // Afficher une notification
    showNotification(`Nouvelle prévision ajoutée par un autre utilisateur`, "success");
    
    // Ajouter la nouvelle ligne au tableau
    addForecastRow(forecast);
});

// Prévision mise à jour
connection.on("ForecastUpdated", (forecast) => {
    console.log("📢 Prévision mise à jour:", forecast);
    
    showNotification(`Prévision #${forecast.id} mise à jour`, "info");
    
    // Mettre à jour la ligne existante
    updateForecastRow(forecast);
});

// Prévision supprimée
connection.on("ForecastDeleted", (id) => {
    console.log("📢 Prévision supprimée:", id);
    
    showNotification(`Prévision #${id} supprimée`, "warning");
    
    // Supprimer la ligne du tableau
    removeForecastRow(id);
});

// ============================================
// GESTION DE LA CONNEXION
// ============================================

connection.onreconnecting((error) => {
    console.warn("⚠️ Reconnexion en cours...", error);
    updateConnectionStatus("reconnecting");
});

connection.onreconnected((connectionId) => {
    console.log("✅ Reconnecté au hub SignalR:", connectionId);
    
    // Mettre à jour le ConnectionId dans le cookie après reconnexion
    if (connectionId) {
        document.cookie = `SignalR-ConnectionId=${connectionId}; path=/; SameSite=Strict; Secure`;
        console.log("📌 ConnectionId mis à jour:", connectionId);
    }
    
    updateConnectionStatus("connected");
});

connection.onclose((error) => {
    console.error("❌ Connexion fermée:", error);
    updateConnectionStatus("disconnected");
});

// Démarrer la connexion
async function startConnection() {
    try {
        await connection.start();
        console.log("✅ Connecté au hub SignalR WeatherForecast");
        
        // Stocker le ConnectionId dans un cookie pour l'exclure des notifications
        const connectionId = connection.connectionId;
        if (connectionId) {
            document.cookie = `SignalR-ConnectionId=${connectionId}; path=/; SameSite=Strict; Secure`;
            console.log("📌 ConnectionId stocké:", connectionId);
        }
        
        updateConnectionStatus("connected");
    } catch (err) {
        console.error("❌ Erreur de connexion SignalR:", err);
        updateConnectionStatus("disconnected");
        // Réessayer après 5 secondes
        setTimeout(startConnection, 5000);
    }
}

// ============================================
// FONCTIONS D'UI
// ============================================

function addForecastRow(forecast) {
    const container = document.getElementById("forecasts-container");
    if (!container) return;
    
    // Vérifier si la carte existe déjà
    const existing = document.querySelector(`div[data-forecast-id="${forecast.id}"]`);
    if (existing) {
        updateForecastRow(forecast);
        return;
    }
    
    const date = new Date(forecast.date).toLocaleDateString('fr-FR');
    const tempF = Math.round((forecast.temperatureC * 9/5) + 32);
    
    // Déterminer le badge de température
    let tempBadge = '';
    if (forecast.temperatureC >= 30) {
        tempBadge = '<span class="badge bg-danger">🔥 Chaud</span>';
    } else if (forecast.temperatureC >= 20) {
        tempBadge = '<span class="badge bg-warning">☀️ Agréable</span>';
    } else if (forecast.temperatureC >= 10) {
        tempBadge = '<span class="badge bg-info">🌤️ Frais</span>';
    } else {
        tempBadge = '<span class="badge bg-primary">❄️ Froid</span>';
    }
    
    const col = document.createElement("div");
    col.className = "col-12 col-md-6 col-lg-4 new-row";
    col.setAttribute("data-forecast-id", forecast.id);
    
    col.innerHTML = `
        <div class="card h-100 weather-card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <div>
                    <h5 class="mb-0">📅 ${date}</h5>
                </div>
                <div>${tempBadge}</div>
            </div>
            <div class="card-body">
                <div class="row text-center mb-3">
                    <div class="col-6">
                        <div class="display-4">🌡️</div>
                        <h3 class="text-primary mb-0">${forecast.temperatureC}°C</h3>
                        <small class="text-muted">${tempF}°F</small>
                    </div>
                    <div class="col-6">
                        <div class="display-4">${forecast.summary === 'Hot' ? '☀️' : forecast.summary === 'Cool' || forecast.summary === 'Freezing' ? '❄️' : '⛅'}</div>
                        <h5 class="mb-0">${forecast.summary || 'N/A'}</h5>
                        <small class="text-muted">Condition</small>
                    </div>
                </div>
                <div class="card-footer bg-transparent border-top-0">
                    <div class="d-grid gap-2">
                        <a href="/WeatherForecast/Details/${forecast.id}" class="btn btn-info btn-sm">
                            🔍 Détails
                        </a>
                        <div class="btn-group" role="group">
                            <a href="/WeatherForecast/Edit/${forecast.id}" class="btn btn-warning btn-sm">
                                ✏️ Modifier
                            </a>
                            <a href="/WeatherForecast/Delete/${forecast.id}" class="btn btn-danger btn-sm">
                                🗑️ Supprimer
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    container.insertBefore(col, container.firstChild);
    
    // Retirer l'animation après 2 secondes
    setTimeout(() => {
        col.classList.remove("new-row");
    }, 2000);
}

function updateForecastRow(forecast) {
    const col = document.querySelector(`div[data-forecast-id="${forecast.id}"]`);
    if (!col) {
        // Si la carte n'existe pas, l'ajouter
        addForecastRow(forecast);
        return;
    }
    
    const date = new Date(forecast.date).toLocaleDateString('fr-FR');
    const tempF = Math.round((forecast.temperatureC * 9/5) + 32);
    
    // Déterminer le badge de température
    let tempBadge = '';
    if (forecast.temperatureC >= 30) {
        tempBadge = '<span class="badge bg-danger">🔥 Chaud</span>';
    } else if (forecast.temperatureC >= 20) {
        tempBadge = '<span class="badge bg-warning">☀️ Agréable</span>';
    } else if (forecast.temperatureC >= 10) {
        tempBadge = '<span class="badge bg-info">🌤️ Frais</span>';
    } else {
        tempBadge = '<span class="badge bg-primary">❄️ Froid</span>';
    }
    
    col.classList.add("updated-row"); // Animation
    
    col.innerHTML = `
        <div class="card h-100 weather-card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <div>
                    <h5 class="mb-0">📅 ${date}</h5>
                </div>
                <div>${tempBadge}</div>
            </div>
            <div class="card-body">
                <div class="row text-center mb-3">
                    <div class="col-6">
                        <div class="display-4">🌡️</div>
                        <h3 class="text-primary mb-0">${forecast.temperatureC}°C</h3>
                        <small class="text-muted">${tempF}°F</small>
                    </div>
                    <div class="col-6">
                        <div class="display-4">${forecast.summary === 'Hot' ? '☀️' : forecast.summary === 'Cool' || forecast.summary === 'Freezing' ? '❄️' : '⛅'}</div>
                        <h5 class="mb-0">${forecast.summary || 'N/A'}</h5>
                        <small class="text-muted">Condition</small>
                    </div>
                </div>
                <div class="card-footer bg-transparent border-top-0">
                    <div class="d-grid gap-2">
                        <a href="/WeatherForecast/Details/${forecast.id}" class="btn btn-info btn-sm">
                            🔍 Détails
                        </a>
                        <div class="btn-group" role="group">
                            <a href="/WeatherForecast/Edit/${forecast.id}" class="btn btn-warning btn-sm">
                                ✏️ Modifier
                            </a>
                            <a href="/WeatherForecast/Delete/${forecast.id}" class="btn btn-danger btn-sm">
                                🗑️ Supprimer
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    setTimeout(() => {
        col.classList.remove("updated-row");
    }, 2000);
}

function removeForecastRow(id) {
    const col = document.querySelector(`div[data-forecast-id="${id}"]`);
    if (col) {
        col.classList.add("deleted-row"); // Animation
        setTimeout(() => {
            col.remove();
        }, 500);
    }
}

function showNotification(message, type = "info") {
    // Créer une notification Bootstrap Toast
    const toastContainer = document.getElementById("toast-container");
    if (!toastContainer) {
        // Créer le container s'il n'existe pas
        const container = document.createElement("div");
        container.id = "toast-container";
        container.className = "toast-container position-fixed bottom-0 end-0 p-3";
        container.style.zIndex = "11";
        document.body.appendChild(container);
    }
    
    const toast = document.createElement("div");
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.setAttribute("role", "alert");
    toast.setAttribute("aria-live", "assertive");
    toast.setAttribute("aria-atomic", "true");
    
    const icon = type === "success" ? "✅" : type === "warning" ? "⚠️" : "ℹ️";
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${icon} ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    
    document.getElementById("toast-container").appendChild(toast);
    
    const bsToast = new bootstrap.Toast(toast, { delay: 3000 });
    bsToast.show();
    
    // Supprimer après fermeture
    toast.addEventListener('hidden.bs.toast', () => {
        toast.remove();
    });
}

function updateConnectionStatus(status) {
    const indicator = document.getElementById("signalr-status");
    if (!indicator) return;
    
    const statusConfig = {
        connected: { text: "🟢 Temps réel activé", class: "badge bg-success" },
        reconnecting: { text: "🟡 Reconnexion...", class: "badge bg-warning" },
        disconnected: { text: "🔴 Déconnecté", class: "badge bg-danger" }
    };
    
    const config = statusConfig[status] || statusConfig.disconnected;
    indicator.textContent = config.text;
    indicator.className = config.class;
}

// ============================================
// DÉMARRAGE
// ============================================

// Démarrer la connexion au chargement de la page
if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", startConnection);
} else {
    startConnection();
}

// Fermer la connexion proprement à la fermeture de la page
window.addEventListener("beforeunload", () => {
    connection.stop();
});
