namespace mobile.Models
{
    /// <summary>
    /// Représente une procédure de démarrage de l'application
    /// </summary>
    public class StartupProcedure
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public StartupProcedureStatus Status { get; set; } = StartupProcedureStatus.Pending;
        public string? ErrorMessage { get; set; }
        public Func<Task<StartupProcedureResult>> ExecuteAsync { get; set; } = null!;
    }

    /// <summary>
    /// Statut d'une procédure de démarrage
    /// </summary>
    public enum StartupProcedureStatus
    {
        Pending,
        Running,
        Success,
        Failed,
        Skipped
    }

    /// <summary>
    /// Résultat de l'exécution d'une procédure
    /// </summary>
    public class StartupProcedureResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool CanContinue { get; set; } = true; // Si false, arrête la queue

        public static StartupProcedureResult Ok() => new() { Success = true };
        public static StartupProcedureResult Fail(string errorMessage, bool canContinue = false) => 
            new() { Success = false, ErrorMessage = errorMessage, CanContinue = canContinue };
    }
}
