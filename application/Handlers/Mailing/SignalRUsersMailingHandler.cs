using domain.Entities;
using domain.Events;
using domain.Events.Mailing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using shared.Hubs;

namespace application.Handlers.Mailing
{
    /// <summary>
    /// Handler qui notifie l'utilisateur via SignalR lors des envois d'emails
    /// </summary>
    public class SignalRUsersMailingHandler :
        INotificationHandler<EmailSentToUser>,
        INotificationHandler<VerificationEmailSentToUser>
    {
        private readonly IHubContext<UsersHub> _usersHub;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SignalRUsersMailingHandler> _logger;

        public SignalRUsersMailingHandler(
            IHubContext<UsersHub> usersHub,
            UserManager<ApplicationUser> userManager,
            ILogger<SignalRUsersMailingHandler> logger)
        {
            _usersHub = usersHub;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task Handle(EmailSentToUser notification, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(notification.ToEmail);
                if (user != null)
                {
                    await _usersHub.Clients.User(user.Id).SendAsync(
                        "EmailSentToUser",
                        new { notification.Subject },
                        cancellationToken);
                }

                // Notifier aussi le groupe par email (pour utilisateurs non authentifiés)
                await _usersHub.Clients.Group(notification.ToEmail).SendAsync(
                    "EmailSentToUser",
                    new { notification.Subject },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la notification SignalR (EmailSentToUser) pour {Email}", notification.ToEmail);
            }
        }

        public async Task Handle(VerificationEmailSentToUser notification, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(notification.ToEmail);
                if (user != null)
                {
                    await _usersHub.Clients.User(user.Id).SendAsync(
                        "VerificationEmailSentToUser",
                        new { Message = "Email de vérification envoyé" },
                        cancellationToken);
                }

                // Notifier aussi le groupe par email (pour utilisateurs non authentifiés)
                await _usersHub.Clients.Group(notification.ToEmail).SendAsync(
                    "VerificationEmailSentToUser",
                    new { Message = "Email de vérification envoyé" },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la notification SignalR (VerificationEmailSentToUser) pour {Email}", notification.ToEmail);
            }
        }
    }
}
