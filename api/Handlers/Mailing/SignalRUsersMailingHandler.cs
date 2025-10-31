using domain.Entities;
using domain.Events;
using domain.Events.Mailing;
using domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;
using System.Text.Json;

namespace api.Handlers.Mailing
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
        private readonly IPendingNotificationService _pending;

        public SignalRUsersMailingHandler(
            IHubContext<UsersHub> usersHub,
            UserManager<ApplicationUser> userManager,
            ILogger<SignalRUsersMailingHandler> logger,
            IPendingNotificationService pending)
        {
            _usersHub = usersHub;
            _userManager = userManager;
            _logger = logger;
            _pending = pending;
        }

        public async Task Handle(EmailSentToUser notification, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(notification.ToEmail);
                var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
                if (user != null)
                {
                    await _usersHub.Clients.User(user.Id).SendAsync(
                        "EmailSentToUser",
                        new { notification.Subject, CorrelationId = correlationId },
                        cancellationToken);
                }

                // Notifier aussi le groupe par email (pour utilisateurs non authentifiés)
                await _usersHub.Clients.Group(notification.ToEmail).SendAsync(
                    "EmailSentToUser",
                    new { notification.Subject, CorrelationId = correlationId },
                    cancellationToken);

                // Bufferiser dans Redis pour rattrapage après redirect/reload
                var payloadJson = JsonSerializer.Serialize(new { notification.Subject, CorrelationId = correlationId });
                await _pending.AddAsync("mail", notification.ToEmail, "EmailSentToUser", payloadJson, TimeSpan.FromMinutes(2), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors de la notification SignalR (EmailSentToUser) pour {Email}", notification.ToEmail);
            }
        }

        public async Task Handle(VerificationEmailSentToUser notification, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(notification.ToEmail);
                var correlationId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
                string notificationText = "Un email de vérification vous a été envoyé.";
                if (user != null)
                {
                    await _usersHub.Clients.User(user.Id).SendAsync(
                        "VerificationEmailSentToUser",
                        new { Message = notificationText, CorrelationId = correlationId },
                        cancellationToken);
                }

                // Notifier aussi le groupe par email (pour utilisateurs non authentifiés)
                await _usersHub.Clients.Group(notification.ToEmail).SendAsync(
                    "VerificationEmailSentToUser",
                    new { Message = notificationText, CorrelationId = correlationId },
                    cancellationToken);

                // Bufferiser dans Redis pour rattrapage
                var payloadJson = JsonSerializer.Serialize(new { Message = notificationText, CorrelationId = correlationId });
                await _pending.AddAsync("mail", notification.ToEmail, "VerificationEmailSentToUser", payloadJson, TimeSpan.FromMinutes(2), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors de la notification SignalR (VerificationEmailSentToUser) pour {Email}", notification.ToEmail);
            }
        }
    }
}
