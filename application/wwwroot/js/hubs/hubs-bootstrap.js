// Bootstrap central pour gérer les connexions multi-hubs
// Chargé globalement dans le layout

// Utilitaires globaux (sanitizer)
import "../utils/html-sanitizer.js";

// Charger notifications pour exposer window.showNotification partout
import "../notifications/notification.js";

// Toujours charger UsersHub (auto-start à l'import)
import "./user-realtime.js";

// Démarrer conditionnellement AdminHub
if (location.pathname.startsWith("/Admin")) {
    import("./admin-realtime.js").then(mod => {
        if (typeof mod.startAdminConnection === "function") {
            mod.startAdminConnection();
        }
    });
}

// Démarrer conditionnellement WeatherForecastHub
if (location.pathname.startsWith("/WeatherForecast")) {
    import("./weatherforecast-realtime.js").then(mod => {
        if (typeof mod.startConnection === "function") {
            mod.startConnection();
        } else if (typeof mod.startWeatherForecastConnection === "function") {
            mod.startWeatherForecastConnection();
        }
    });
}
