using domain.Constants;
using domain.Events;
using domain.Events.Mailing;
using domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace domain.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IPublisher _publisher;
        private readonly EmailOptions _options;

        public EmailService(ILogger<EmailService> logger, IPublisher publisher, IOptions<EmailOptions> options)
        {
            _logger = logger;
            _publisher = publisher;
            _options = options.Value;
        }

        private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.From))
                {
                    _logger.LogWarning("[Email] Configuration incomplète. Host/From requis. Log-only: To={To} | Subject={Subject}", toEmail, subject);
                    return;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(_options.From);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(_options.UserName, _options.Password)
                };

#if NET6_0_OR_GREATER
                await client.SendMailAsync(message, cancellationToken);
#else
                await client.SendMailAsync(message);
#endif

                _logger.LogInformation("[Email] Envoyé à {To} | Subject={Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi d'un email à {Email}", toEmail);
                throw;
            }
        }

        private async Task SendBulkAsync(IEnumerable<string> toEmails, string subject, string body, CancellationToken cancellationToken = default)
        {
            foreach (var toEmail in toEmails)
            {
                try
                {
                    await SendAsync(toEmail, subject, body, cancellationToken);
                    _logger.LogInformation("[Email] To={To} | Subject={Subject} | Body={Body}", toEmail, subject, body);
                    await _publisher.Publish(new EmailSentToUser(toEmail, subject, body), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'envoi d'un email en masse à {Email}", toEmail);
                    // Continue avec les autres destinataires
                }
            }
        }

        public async Task SendEmailConfirmationAsync(string toEmail, CancellationToken cancellationToken = default)
        {
            // NOTE: Ici on simule un envoi. En prod: générer un token & URL et utiliser un provider SMTP/SendGrid/etc.
            var subject = "Confirmez votre adresse email";
            var body = "Merci pour votre inscription. Cliquez sur le lien de confirmation envoyé par l'application (démo).";
            // await SendAsync(toEmail, subject, body, cancellationToken);
            await _publisher.Publish(new VerificationEmailSentToUser(toEmail), cancellationToken);
        }

        public async Task SendEmailForgotPasswordAsync(string toEmail, CancellationToken cancellationToken = default)
        {
            // NOTE: Ici on simule un envoi. En prod: générer un token & URL et utiliser un provider SMTP/SendGrid/etc.
            var subject = "Mot de passe oublié";
            var body = "Vous avez demandé un nouveau mot de passe. Cliquez sur le lien de confirmation envoyé par l'application (démo).";
            // await SendAsync(toEmail, subject, body, cancellationToken);
            await _publisher.Publish(new VerificationEmailSentToUser(toEmail), cancellationToken);
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string body, CancellationToken cancellationToken = default)
        {
            // NOTE: Ici on simule un envoi. En prod: générer un token & URL et utiliser un provider SMTP/SendGrid/etc.
            await SendBulkAsync(toEmails, subject, body, cancellationToken);
        }
    }
}
