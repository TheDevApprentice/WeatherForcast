using mobile.Services;
using Microsoft.Extensions.Logging;

namespace mobile.Extensions
{
    /// <summary>
    /// Extensions pour faciliter la gestion d'erreurs sur les tâches
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Exécute une tâche avec gestion d'erreurs automatique
        /// </summary>
        public static async Task SafeExecuteAsync(
            this Task task,
            IErrorHandler errorHandler,
            string? context = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                await errorHandler.HandleErrorAsync(ex, context);
            }
        }

        /// <summary>
        /// Exécute une tâche avec gestion d'erreurs automatique et retourne un résultat
        /// </summary>
        public static async Task<T?> SafeExecuteAsync<T>(
            this Task<T> task,
            IErrorHandler errorHandler,
            string? context = null,
            T? defaultValue = default)
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                await errorHandler.HandleErrorAsync(ex, context);
                return defaultValue;
            }
        }

        /// <summary>
        /// Exécute une action avec gestion d'erreurs automatique
        /// </summary>
        public static async Task SafeExecuteAsync(
            Func<Task> action,
            IErrorHandler errorHandler,
            string? context = null)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleErrorAsync(ex, context);
            }
        }

        /// <summary>
        /// Exécute une fonction avec gestion d'erreurs automatique et retourne un résultat
        /// </summary>
        public static async Task<T?> SafeExecuteAsync<T>(
            Func<Task<T>> func,
            IErrorHandler errorHandler,
            string? context = null,
            T? defaultValue = default)
        {
            try
            {
                return await func();
            }
            catch (Exception ex)
            {
                await errorHandler.HandleErrorAsync(ex, context);
                return defaultValue;
            }
        }
    }
}
