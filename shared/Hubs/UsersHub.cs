using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace shared.Hubs
{
    // Hub destiné aux notifications utilisateur (authentifiés et non authentifiés)
    [AllowAnonymous]
    public class UsersHub : Hub
    {
        /// <summary>
        /// Permet à un client de rejoindre un canal basé sur l'email
        /// Utile pour notifier un utilisateur non connecté juste après l'inscription/verification
        /// </summary>
        public Task JoinEmailChannel(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Task.CompletedTask;

            return Groups.AddToGroupAsync(Context.ConnectionId, email);
        }

        /// <summary>
        /// Permet de quitter le canal email
        /// </summary>
        public Task LeaveEmailChannel(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Task.CompletedTask;

            return Groups.RemoveFromGroupAsync(Context.ConnectionId, email);
        }
    }
}
