// Utilitaire d'échappement HTML minimaliste et sûr
// Usage: HtmlSanitizer.sanitize(value)

export function sanitize(input) {
    const str = input == null ? "" : String(input);
    // Remplacer les caractères spéciaux HTML
    return str
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

// Namespace global pour usage sans bundler
if (typeof window !== "undefined") {
    window.HtmlSanitizer = window.HtmlSanitizer || {};
    if (!window.HtmlSanitizer.sanitize) {
        window.HtmlSanitizer.sanitize = sanitize;
    }
}
