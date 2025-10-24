using domain.Events;
using domain.Events.Admin;
using domain.Interfaces.Services;

namespace application.Handlers.Mailing
{
    public class SendEmailHandler : 
        INotificationHandler<UserRegisteredEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<SendEmailHandler> _logger;

        public SendEmailHandler(
            IEmailService emailService,
            ILogger<SendEmailHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _emailService.SendEmailConfirmationAsync(notification.Email, cancellationToken);
                _logger.LogInformation("Email de vérification envoyé à {Email}", notification.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de vérification pour {Email}", notification.Email);
            }
        }
    }
}
