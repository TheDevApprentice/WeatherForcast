using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace mobile.Helpers
{
    /// <summary>
    /// Helper pour les logs conditionnels (DEBUG uniquement)
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Log Debug (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogDebug(
            this ILogger logger,
            string message,
            params object[] args)
        {
            logger.LogDebug(message, args);
        }

        /// <summary>
        /// Log Information (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogDebugInfo(
            this ILogger logger,
            string message,
            params object[] args)
        {
            logger.LogInformation(message, args);
        }

        /// <summary>
        /// Log avec contexte de méthode (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogDebugMethod(
            this ILogger logger,
            string message = "",
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            logger.LogDebug(
                "[{FileName}.{MethodName}:{LineNumber}] {Message}",
                fileName,
                methodName,
                lineNumber,
                message);
        }

        /// <summary>
        /// Log une entrée de méthode (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogMethodEntry(
            this ILogger logger,
            [CallerMemberName] string methodName = "")
        {
            logger.LogDebug("→ Entering {MethodName}", methodName);
        }

        /// <summary>
        /// Log une sortie de méthode (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogMethodExit(
            this ILogger logger,
            [CallerMemberName] string methodName = "")
        {
            logger.LogDebug("← Exiting {MethodName}", methodName);
        }

        /// <summary>
        /// Log un objet en JSON (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogDebugObject<T>(
            this ILogger logger,
            string name,
            T obj)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                logger.LogDebug("{Name}: {Json}", name, json);
            }
            catch
            {
                logger.LogDebug("{Name}: {Object}", name, obj?.ToString() ?? "null");
            }
        }

        /// <summary>
        /// Écrit dans la console (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void WriteDebug(string message)
        {
            Debug.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} - {message}");
        }

        /// <summary>
        /// Écrit dans la console avec couleur (uniquement en mode DEBUG)
        /// </summary>
        [Conditional("DEBUG")]
        public static void WriteDebugColored(string message, ConsoleColor color = ConsoleColor.Cyan)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} - {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}
