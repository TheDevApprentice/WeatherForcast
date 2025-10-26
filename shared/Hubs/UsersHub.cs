using domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace shared.Hubs
{
    // Hub destiné aux notifications utilisateur (authentifiés et non authentifiés)
    [AllowAnonymous]
    public class UsersHub : Hub
    {
        private readonly IPendingNotificationService _pending;
        private readonly ILogger<UsersHub> _logger;

        public UsersHub(IPendingNotificationService pending, ILogger<UsersHub> logger)
        {
            _pending = pending;
            _logger = logger;
        }

        /// <summary>
        /// Permet à un client de rejoindre un canal basé sur l'email
        /// Utile pour notifier un utilisateur non connecté juste après l'inscription/verification
        /// </summary>
        public Task JoinEmailChannel(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("[UsersHub] JoinEmailChannel: email vide pour ConnId={ConnId}", Context.ConnectionId);
                return Task.CompletedTask;
            }

            _logger.LogInformation("[UsersHub] JoinEmailChannel: {Email} ConnId={ConnId}", email, Context.ConnectionId);
            return Groups.AddToGroupAsync(Context.ConnectionId, email);
        }

        /// <summary>
        /// Permet de quitter le canal email
        /// </summary>
        public Task LeaveEmailChannel(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("[UsersHub] LeaveEmailChannel: email vide pour ConnId={ConnId}", Context.ConnectionId);
                return Task.CompletedTask;
            }

            _logger.LogInformation("[UsersHub] LeaveEmailChannel: {Email} ConnId={ConnId}", email, Context.ConnectionId);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, email);
        }

        /// <summary>
        /// Permet à un utilisateur authentifié de rejoindre son groupe personnel
        /// Utilisé pour les notifications spécifiques à l'utilisateur (révocation de session, etc.)
        /// </summary>
        public Task JoinUserGroup(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[UsersHub] JoinUserGroup: userId vide pour ConnId={ConnId}", Context.ConnectionId);
                return Task.CompletedTask;
            }

            _logger.LogInformation("[UsersHub] JoinUserGroup: User_{UserId} ConnId={ConnId}", userId, Context.ConnectionId);
            return Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        /// <summary>
        /// Permet de quitter le groupe utilisateur
        /// </summary>
        public Task LeaveUserGroup(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[UsersHub] LeaveUserGroup: userId vide pour ConnId={ConnId}", Context.ConnectionId);
                return Task.CompletedTask;
            }

            _logger.LogInformation("[UsersHub] LeaveUserGroup: User_{UserId} ConnId={ConnId}", userId, Context.ConnectionId);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        /// <summary>
        /// Récupère les notifications en attente pour cet email puis purge le buffer.
        /// </summary>
        public async Task<object[]> FetchPendingMailNotifications(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("[UsersHub] FetchPendingMailNotifications: email vide pour ConnId={ConnId}", Context.ConnectionId);
                return Array.Empty<object>();
            }

            _logger.LogInformation("[UsersHub] FetchPendingMailNotifications: {Email} ConnId={ConnId}", email, Context.ConnectionId);
            var items = await _pending.FetchPendingAsync("mail", email);
            // Retourne un tableau d'objets anonymes { type, payload }
            return items.Select(i => new { type = i.Type, payload = i.PayloadJson }).Cast<object>().ToArray();
        }

        /// <summary>
        /// Récupère les notifications en attente (erreurs, etc.) pour un utilisateur puis purge le buffer.
        /// </summary>
        public async Task<object[]> GetPendingNotifications(string notificationType, string userId)
        {
            if (string.IsNullOrWhiteSpace(notificationType) || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[UsersHub] GetPendingNotifications: paramètres invalides pour ConnId={ConnId}", Context.ConnectionId);
                return Array.Empty<object>();
            }

            _logger.LogInformation("[UsersHub] GetPendingNotifications: Type={Type} UserId={UserId} ConnId={ConnId}", 
                notificationType, userId, Context.ConnectionId);
            
            var items = await _pending.FetchPendingAsync(notificationType, userId);
            // Retourne un tableau d'objets anonymes { type, payload }
            return items.Select(i => new { type = i.Type, payload = i.PayloadJson }).Cast<object>().ToArray();
        }
    }
}
