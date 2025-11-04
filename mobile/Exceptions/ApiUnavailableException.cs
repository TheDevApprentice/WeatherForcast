namespace mobile.Exceptions
{
    /// <summary>
    /// Exception levée lorsque l'API n'est pas joignable (erreur réseau, bad gateway, etc.)
    /// Utilisée pour activer le mode offline
    /// </summary>
    public class ApiUnavailableException : Exception
    {
        public ApiUnavailableException() : base("L'API n'est pas joignable")
        {
        }

        public ApiUnavailableException(string message) : base(message)
        {
        }

        public ApiUnavailableException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
