// ============================================
// SIGNALR - NOTIFICATIONS EN TEMPS RÉEL
// ============================================

// Importe showNotification (nécessite <script type="module">)
import { showNotification } from "./notifications/notification.js";
import { updateConnectionStatus } from "./utils/connection-status.js";

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
    showNotification(`Nouvelle prévision`, `${forecast.date} - ${forecast.summary} - ${forecast.temperatureC}°C`, "success");
    
    // Ajouter la nouvelle ligne au tableau
    addForecastRow(forecast);
});

// Prévision mise à jour
connection.on("ForecastUpdated", (forecast) => {
    console.log("📢 Prévision mise à jour:", forecast);
    
    const details = `${forecast.date} - ${forecast.summary} - ${forecast.temperatureC}°C (id ${forecast.id})`;
    showNotification("Prévision mise à jour", details, "info");
    
    // Mettre à jour la ligne existante
    updateForecastRow(forecast);
});

// Prévision supprimée
connection.on("ForecastDeleted", (id) => {
    console.log("📢 Prévision supprimée:", id);
    
    showNotification("Prévision supprimée", `Prévision #${id}`, "warning");
    
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
export async function startConnection() {
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



// ============================================
// DÉMARRAGE
// ============================================

// Le démarrage est désormais piloté par js/hubs-bootstrap.js

// Fermer la connexion proprement à la fermeture de la page
window.addEventListener("beforeunload", () => {
    connection.stop();
});
