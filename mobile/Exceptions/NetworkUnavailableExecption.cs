namespace mobile.Exceptions
{
    /// <summary>
    /// Exception de base pour toutes les exceptions métier de l'application
    /// </summary>
    public class NetworkUnavailableExecption (
        string userMessage = "Réseau non disponible vous être hors ligne",
        string? technicalMessage = "Network is unavailable",
        Exception? innerException = null) : 
        NetworkException (technicalMessage, innerException)
    {
        public new string UserMessage { get; } = userMessage;
    }
}
