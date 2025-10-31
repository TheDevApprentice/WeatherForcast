using domain.Events;
using domain.Events.Mailing;

namespace api.Handlers.Mailing
{
    /// <summary>
    /// Handler qui log les événements d'emailing (audit)
    /// </summary>
    public class AuditLogMailingHandler :
        INotificationHandler<EmailSentToUser>,
        INotificationHandler<VerificationEmailSentToUser>
    {
        private readonly ILogger<AuditLogMailingHandler> _logger;

        public AuditLogMailingHandler(ILogger<AuditLogMailingHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(EmailSentToUser notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("📧 API - [Audit Email] Sent to {Email} | Subject: {Subject}",
                notification.ToEmail,
                notification.Subject);
            return Task.CompletedTask;
        }

        public Task Handle(VerificationEmailSentToUser notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("✅ API - [Audit Email] Verification email sent to {Email}",
                notification.ToEmail);
            return Task.CompletedTask;
        }
    }
}
