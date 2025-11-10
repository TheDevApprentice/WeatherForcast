namespace mobile.Exceptions
{
    /// <summary>
    /// Exception levée lorsque l'API n'est pas joignable (erreur réseau, bad gateway, etc.)
    /// Utilisée pour activer le mode offline
    /// </summary>
    public class ApiUnavailableException : NetworkException
    {
        public ApiUnavailableException() 
            : base("L'API n'est pas joignable", null)
        {
        }

        public ApiUnavailableException(string technicalMessage) 
            : base(technicalMessage, null)
        {
        }

        public ApiUnavailableException(string technicalMessage, Exception innerException) 
            : base(technicalMessage, innerException)
        {
        }
    }
}
