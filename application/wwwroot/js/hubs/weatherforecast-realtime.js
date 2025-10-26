// ============================================
// SIGNALR - NOTIFICATIONS EN TEMPS RÉEL
// ============================================

// Importe showNotification (nécessite <script type="module">)
import { showNotification } from "../notifications/notification.js";
import { updateConnectionStatus } from "../utils/connection-status.js";

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

function clearElement(el) {
    while (el.firstChild) el.removeChild(el.firstChild);
}

function el(tag, className, text) {
    const e = document.createElement(tag);
    if (className) e.className = className;
    if (text !== undefined && text !== null) e.textContent = String(text);
    return e;
}

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
    let tempBadgeSpan;
    if (forecast.temperatureC >= 30) {
        tempBadgeSpan = el("span", "badge bg-danger", "🔥 Chaud");
    } else if (forecast.temperatureC >= 20) {
        tempBadgeSpan = el("span", "badge bg-warning", "☀️ Agréable");
    } else if (forecast.temperatureC >= 10) {
        tempBadgeSpan = el("span", "badge bg-info", "🌤️ Frais");
    } else {
        tempBadgeSpan = el("span", "badge bg-primary", "❄️ Froid");
    }
    
    const col = document.createElement("div");
    col.className = "col-12 col-md-6 col-lg-4 new-row";
    col.setAttribute("data-forecast-id", forecast.id);
    
    const card = el("div", "card h-100 weather-card");
    const header = el("div", "card-header d-flex justify-content-between align-items-center");
    const headerLeft = document.createElement("div");
    const h5 = el("h5", "mb-0", `📅 ${date}`);
    headerLeft.appendChild(h5);
    const headerRight = document.createElement("div");
    headerRight.appendChild(tempBadgeSpan);
    header.appendChild(headerLeft);
    header.appendChild(headerRight);

    const body = el("div", "card-body");
    const row = el("div", "row text-center mb-3");
    const colLeft = el("div", "col-6");
    colLeft.appendChild(el("div", "display-4", "🌡️"));
    colLeft.appendChild(el("h3", "text-primary mb-0", `${forecast.temperatureC}°C`));
    colLeft.appendChild(el("small", "text-muted", `${tempF}°F`));
    const colRight = el("div", "col-6");
    const emoji = forecast.summary === 'Hot' ? '☀️' : (forecast.summary === 'Cool' || forecast.summary === 'Freezing' ? '❄️' : '⛅');
    colRight.appendChild(el("div", "display-4", emoji));
    colRight.appendChild(el("h5", "mb-0", forecast.summary || 'N/A'));
    colRight.appendChild(el("small", "text-muted", "Condition"));
    row.appendChild(colLeft);
    row.appendChild(colRight);
    body.appendChild(row);

    const footer = el("div", "card-footer bg-transparent border-top-0");
    const grid = el("div", "d-grid gap-2");
    const details = document.createElement("a");
    details.href = `/WeatherForecast/Details/${forecast.id}`;
    details.className = "btn btn-info btn-sm";
    details.textContent = "🔍 Détails";
    grid.appendChild(details);
    const group = el("div", "btn-group", null);
    group.setAttribute("role", "group");
    const edit = document.createElement("a");
    edit.href = `/WeatherForecast/Edit/${forecast.id}`;
    edit.className = "btn btn-warning btn-sm";
    edit.textContent = "✏️ Modifier";
    const del = document.createElement("a");
    del.href = `/WeatherForecast/Delete/${forecast.id}`;
    del.className = "btn btn-danger btn-sm";
    del.textContent = "🗑️ Supprimer";
    group.appendChild(edit);
    group.appendChild(del);
    grid.appendChild(group);
    footer.appendChild(grid);

    card.appendChild(header);
    card.appendChild(body);
    card.appendChild(footer);

    col.appendChild(card);
    
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

    clearElement(col);

    const card = el("div", "card h-100 weather-card");
    const header = el("div", "card-header d-flex justify-content-between align-items-center");
    const headerLeft = document.createElement("div");
    const h5 = el("h5", "mb-0", `📅 ${date}`);
    headerLeft.appendChild(h5);
    const headerRight = document.createElement("div");
    headerRight.appendChild(tempBadgeSpan);
    header.appendChild(headerLeft);
    header.appendChild(headerRight);

    const body = el("div", "card-body");
    const row = el("div", "row text-center mb-3");
    const colLeft = el("div", "col-6");
    colLeft.appendChild(el("div", "display-4", "🌡️"));
    colLeft.appendChild(el("h3", "text-primary mb-0", `${forecast.temperatureC}°C`));
    colLeft.appendChild(el("small", "text-muted", `${tempF}°F`));
    const colRight = el("div", "col-6");
    const emoji = forecast.summary === 'Hot' ? '☀️' : (forecast.summary === 'Cool' || forecast.summary === 'Freezing' ? '❄️' : '⛅');
    colRight.appendChild(el("div", "display-4", emoji));
    colRight.appendChild(el("h5", "mb-0", forecast.summary || 'N/A'));
    colRight.appendChild(el("small", "text-muted", "Condition"));
    row.appendChild(colLeft);
    row.appendChild(colRight);
    body.appendChild(row);

    const footer = el("div", "card-footer bg-transparent border-top-0");
    const grid = el("div", "d-grid gap-2");
    const details = document.createElement("a");
    details.href = `/WeatherForecast/Details/${forecast.id}`;
    details.className = "btn btn-info btn-sm";
    details.textContent = "🔍 Détails";
    grid.appendChild(details);
    const group = el("div", "btn-group", null);
    group.setAttribute("role", "group");
    const edit = document.createElement("a");
    edit.href = `/WeatherForecast/Edit/${forecast.id}`;
    edit.className = "btn btn-warning btn-sm";
    edit.textContent = "✏️ Modifier";
    const del = document.createElement("a");
    del.href = `/WeatherForecast/Delete/${forecast.id}`;
    del.className = "btn btn-danger btn-sm";
    del.textContent = "🗑️ Supprimer";
    group.appendChild(edit);
    group.appendChild(del);
    grid.appendChild(group);
    footer.appendChild(grid);

    card.appendChild(header);
    card.appendChild(body);
    card.appendChild(footer);

    col.appendChild(card);
    
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
